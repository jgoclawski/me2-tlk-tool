﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;

namespace Mass_Effect_2_TLK_Tool
{
    class HuffmanCompression
    {
        private Version _inputFileVersion = new Version("1.0.0.0");
        private List<TLKEntry> _inputData = new List<TLKEntry>();
        private Dictionary<char, int> frequencyCount = new Dictionary<char, int>();
        private List<HuffmanNode> _huffmanTree = new List<HuffmanNode>();
        private Dictionary<char, BitArray> _huffmanCodes = new Dictionary<char, BitArray>();

        private class TLKEntry : IComparable
        {
            public int StringID;
            public int position;
            public String data;

            public TLKEntry(int StringID, int position, String data)
            {
                this.StringID = StringID;
                this.position = position;
                this.data = data;
            }

            public int CompareTo(object obj)
            {
               TLKEntry entry = (TLKEntry)obj;
               return position.CompareTo(entry.position);
            }
        }

        private class HuffmanNode
        {
            public char Data;
            public readonly int FrequencyCount;
            public HuffmanNode Left;
            public HuffmanNode Right;

            public int ID;

            public HuffmanNode(char d, int freq)
            {
                Data = d;
                FrequencyCount = freq;
            }

            public HuffmanNode(HuffmanNode left, HuffmanNode right)
            {
                FrequencyCount = left.FrequencyCount + right.FrequencyCount;
                Left = left;
                Right = right;
            }
        }

        /// <summary>
        /// Loads a file into memory and prepares for compressing it to TLK
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="ff"></param>
        /// <param name="debugVersion"></param>
        public void LoadInputData(string fileName, TalkFile.Fileformat ff, bool debugVersion)
        {
            _inputData.Clear();
            LoadXmlInputData(fileName, debugVersion);
            _inputData.Sort();
            PrepareHuffmanCoding();
        }

        /// <summary>
        /// Dumps data from memory to TLK compressed file format.
        /// <remarks>
        /// Compressed data should be read into memory first, by LoadInputData method.
        /// </remarks>
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="isPC"></param>
        public void SaveToTlkFile(string fileName, bool isPC)
        {
            File.Delete(fileName);

            /* converts Huffmann Tree to binary form */
            List<Int32> treeBuffer = ConvertHuffmanTreeToBuffer();

            /* preparing data and entries for writing to file
             * entries list consists of pairs <String ID, Offset> */
            List<BitArray> binaryData = new List<BitArray>();
            Dictionary<Int32, Int32> entries1 = new Dictionary<Int32, Int32>();
            Dictionary<Int32, Int32> entries2 = new Dictionary<Int32, Int32>();
            int offset = 0;

            foreach (var entry in _inputData)
            {
                if (entry.StringID < 0)
                {
                    if (!entries1.ContainsKey(entry.StringID))
                        entries1.Add(entry.StringID, Convert.ToInt32(entry.data));
                    else
                        entries2.Add(entry.StringID, Convert.ToInt32(entry.data));
                    continue;
                }

                if (!entries1.ContainsKey(entry.StringID))
                    entries1.Add(entry.StringID, offset);
                else
                    entries2.Add(entry.StringID, offset);

                /* for every character in a string, put it's binary code into data array */
                foreach (char c in entry.data)
                {
                    binaryData.Add(_huffmanCodes[c]);
                    offset += _huffmanCodes[c].Count;
                }
            }

            /* preparing TLK Header */
            Int32 magic = 7040084;
            Int32 ver = 3;
            Int32 min_ver = 2;
            Int32 entry1Count = entries1.Count;
            Int32 entry2Count = entries2.Count;
            Int32 treeNodeCount = treeBuffer.Count() / 2;
            Int32 dataLength = offset / 8;
            if (offset % 8 > 0)
                ++dataLength;

            /* using LittleEndian for PC architecture and BigEndian for Xbox360 */
            MiscUtil.Conversion.EndianBitConverter bitConverter;
            if (isPC)
                bitConverter = new MiscUtil.Conversion.LittleEndianBitConverter();
            else
                bitConverter = new MiscUtil.Conversion.BigEndianBitConverter();

            var bw = new MiscUtil.IO.EndianBinaryWriter(bitConverter, File.OpenWrite(fileName));

            /* writing TLK Header */
            bw.Write(magic);
            bw.Write(ver);
            bw.Write(min_ver);
            bw.Write(entry1Count);
            bw.Write(entry2Count);
            bw.Write(treeNodeCount);
            bw.Write(dataLength);

            /* writing entries */
            foreach (var entry in entries1)
            {
                bw.Write(entry.Key);
                bw.Write(entry.Value);
            }
            foreach (var entry in entries2)
            {
                bw.Write(entry.Key);
                bw.Write(entry.Value);
            }

            /* writing HuffmanTree */
            foreach (Int32 element in treeBuffer)
            {
                bw.Write(element);
            }

            /* writing data */
            byte[] data = BitArrayListToByteArray(binaryData, offset);
            bw.Write(data);

            bw.Close();
        }

        /// <summary>
        /// Loads data from XML file into memory
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="debugVersion"></param>
        private void LoadXmlInputData(string fileName, bool debugVersion)
        {
            var xmlReader = new XmlTextReader(fileName);

            /* read and store TLK Tool version, which was used to create the XML file */
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "tlkFile")
                {
                    string toolVersion = xmlReader.GetAttribute("TLKToolVersion");
                    if (toolVersion != null)
                        _inputFileVersion = new Version(toolVersion);
                    break;
                }
            }

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "string")
                {
                    int id = 0, position = 0;
                    string data = "";
                    while (xmlReader.Name != "string" || xmlReader.NodeType != XmlNodeType.EndElement)
                    {
                        if (!xmlReader.Read() || xmlReader.NodeType != XmlNodeType.Element)
                            continue;
                        if (xmlReader.Name == "id")
                            id = xmlReader.ReadElementContentAsInt();
                        else if (xmlReader.Name == "position")
                            position = xmlReader.ReadElementContentAsInt();
                        else if (xmlReader.Name == "data")
                            data = xmlReader.ReadString();
                    }
                    data = data.Replace("\r\n", "\n");
                    /* every string should be NULL-terminated */
                    if (id >= 0)
                        data += '\0';
                    /* only add debug info if we are in debug mode and StringID is positive AND it's localizable */
                    if (id >= 0 && debugVersion && (id & 0x8000000) != 0x8000000)
                        _inputData.Add(new TLKEntry(id, position, "(#" + id + ") " + data));
                    else
                        _inputData.Add(new TLKEntry(id, position, data));
                }
            }
            xmlReader.Close();

            /* code for XML files created BEFORE v. 1.0.3 */
            Version lastEntryFixVersion = new Version("1.0.3");

            /* check if someone isn't loading the bugged version < 1.0.3 */
            if (_inputFileVersion < lastEntryFixVersion)
            {
                MessageBox.Show(
                    Properties.Resources.AlertPre103XML, Properties.Resources.Warning,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /* maybe will be finished in the future */
        private void LoadTxtInputData(string fileName)
        {
            StreamReader streamReader = new StreamReader(fileName);
            string line;
            int i = 1;

            while (streamReader.Peek() != -1)
            {
                line = streamReader.ReadLine();
                Console.WriteLine(i++);
                Console.WriteLine(line);
                char[] delimiterChars = { ':' };
                string[] words = line.Split(delimiterChars);
                Console.WriteLine("{0} words in text:", words.Length);
                foreach (string s in words)
                {
                    Console.Write(s + " | ");
                }
                Console.WriteLine();
            }
            streamReader.Close();
        }

        /// <summary>
        /// Creates Huffman Tree based on data from memory.
        /// For every character in text data, a corresponding Huffman Code is prepared.
        /// Source: http://en.wikipedia.org/wiki/Huffman_coding
        /// </summary>
        private void PrepareHuffmanCoding()
        {
            frequencyCount.Clear();
            foreach (var entry in _inputData)
            {
                if (entry.StringID < 0)
                    continue;
                foreach (char c in entry.data)
                {
                    if (!frequencyCount.ContainsKey(c))
                        frequencyCount.Add(c, 0);
                    ++frequencyCount[c];
                }
            }

            foreach (var element in frequencyCount)
                _huffmanTree.Add(new HuffmanNode(element.Key, element.Value));

            BuildHuffmanTree();
            BuildCodingArray();
           
            // DebugTools.LoadHuffmanTree(_huffmanCodes);
            // DebugTools.PrintLookupTable();
        }

        /// <summary>
        /// Standard implementation of builidng a Huffman Tree
        /// </summary>
        private void BuildHuffmanTree()
        {
            while (_huffmanTree.Count() > 1)
            {
                /* sort Huffman Nodes by frequency */
                _huffmanTree.Sort(CompareNodes);

                HuffmanNode parent = new HuffmanNode(_huffmanTree[0], _huffmanTree[1]);
                _huffmanTree.RemoveAt(0);
                _huffmanTree.RemoveAt(0);
                _huffmanTree.Add(parent);
            }
        }

        /// <summary>
        /// Using Huffman Tree (created with BuildHuffmanTree method), generates a binary code for every character.
        /// </summary>
        private void BuildCodingArray()
        {
            /* stores a binary code */
            List<bool> currentCode = new List<bool>();
            HuffmanNode currenNode = _huffmanTree[0];

            TraverseHuffmanTree(currenNode, currentCode);
        }

        /// <summary>
        /// Recursively traverses Huffman Tree and generates codes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="code"></param>
        private void TraverseHuffmanTree(HuffmanNode node, List<bool> code)
        {
            /* check if both sons are null */
            if (node.Left == node.Right)
            {
                BitArray ba = new BitArray(code.ToArray());
                _huffmanCodes.Add(node.Data, ba);
            }
            else
            {
                /* adds 0 to the code - process left son*/
                code.Add(false);
                TraverseHuffmanTree(node.Left, code);
                code.RemoveAt(code.Count() - 1);

                /* adds 1 to the code - process right son*/
                code.Add(true);
                TraverseHuffmanTree(node.Right, code);
                code.RemoveAt(code.Count() - 1);
            }
        }

        /// <summary>
        /// Converts a Huffman Tree to it's binary representation used by TLK format of Mass Effect 2.
        /// </summary>
        /// <returns></returns>
        private List<Int32> ConvertHuffmanTreeToBuffer()
        {
            Queue <HuffmanNode> q = new Queue<HuffmanNode>();
            Dictionary<int, HuffmanNode> indices = new Dictionary<int, HuffmanNode>();

            int index = 0;
            q.Enqueue(_huffmanTree[0]);

            while (q.Count > 0)
            {
                HuffmanNode node = q.Dequeue();
                /* if it's a leaf - set it's ID to reflect char data the node contains */
                if (node.Left == node.Right)
                {
                    /* store the char data */
                    node.ID = -1 - node.Data;

                    /* that's how it's going to be decoded when parsing TLK file:
                     * char c = BitConverter.ToChar(BitConverter.GetBytes(0xffff - node.ID), 0); */
                } 
                else
                {
                    node.ID = index++;
                    indices.Add(node.ID, node);
                }
                if (node.Right != null)
                    q.Enqueue(node.Right);
                if (node.Left != null)
                    q.Enqueue(node.Left);
            }

            List<Int32> output = new List<Int32>();

            foreach (HuffmanNode node in indices.Values)
            {
                output.Add((Int32)node.Left.ID);
                output.Add((Int32)node.Right.ID);
            }

            return output;
        }

        /// <summary>
        /// Converts bits in a BitArray to an array with bytes.
        /// Such array is ready to be written to a file.
        /// </summary>
        /// <param name="bitsList"></param>
        /// <param name="bitsCount"></param>
        /// <returns></returns>
        private static byte[] BitArrayListToByteArray(List<BitArray> bitsList, int bitsCount)
        {
            const int BITSPERBYTE = 8;

            int bytesize = bitsCount / BITSPERBYTE;
            if (bitsCount % BITSPERBYTE > 0)
                bytesize++;

            byte[] bytes = new byte[bytesize];
            int bytepos = 0;
            int bitsRead = 0;
            byte value = 0;
            byte significance = 1;

            foreach (BitArray bits in bitsList)
            {
                int bitpos = 0;

                while (bitpos < bits.Length)
                {
                    if (bits[bitpos])
                    {
                        value += significance;
                    }
                    ++bitpos;
                    ++bitsRead;
                    if (bitsRead % BITSPERBYTE == 0)
                    {
                        bytes[bytepos] = value;
                        ++bytepos;
                        value = 0;
                        significance = 1;
                        bitsRead = 0;
                    }
                    else
                    {
                        significance <<= 1;
                    }
                }
            }
            if (bitsRead % BITSPERBYTE != 0)
                bytes[bytepos] = value;
            return bytes;
        }

        /// <summary>
        /// For sorting Huffman Nodes
        /// </summary>
        /// <param name="L1"></param>
        /// <param name="L2"></param>
        /// <returns></returns>
        private static int CompareNodes(HuffmanNode L1, HuffmanNode L2)
        {
            return L1.FrequencyCount.CompareTo(L2.FrequencyCount);
        }
    }
}
