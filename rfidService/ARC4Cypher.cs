using System;
using System.Collections.Generic;
using System.Text;

namespace Chypher
{
    /// <summary>
    /// 
    /// Obtenido de http://en.wikipedia.org/wiki/RC4
    /// RC4 generates a pseudorandom stream of bits (a keystream) which, for encryption, is combined with the plaintext using bit-wise exclusive-or; decryption is performed the same way (since exclusive-or is a symmetric operation). (This is similar to the Vernam cipher except that generated pseudorandom bits, rather than a prepared stream, are used.) To generate the keystream, the cipher makes use of a secret internal state which consists of two parts:
    ///
    ///        1.A permutation of all 256 possible bytes (denoted "S" below).
    ///        2.Two 8-bit index-pointers (denoted "i" and "j").
    /// The permutation is initialized with a variable length key, typically between 40 and 256 bits, using the key-scheduling algorithm (KSA). Once this has been completed, the stream of bits is generated using the pseudo-random generation algorithm (PRGA).
    /// 
    /// </summary>
    public class ARC4Cypher        
    {
        const int STATE_ARRAY_SIZE = 256;

        private byte[] S=new byte[STATE_ARRAY_SIZE];
        private uint index_i, index_j;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        private  void swap(byte[] s, uint i, uint j)
        {
            byte temp = s[i];
            s[i] = s[j];
            s[j] = temp;
        }

        /// <summary>
        ///  KSA 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="key_length"></param>
        private   void rc4_init(byte[] key) 
        {
            for (uint i = 0; i < STATE_ARRAY_SIZE; i++) S[i] =(byte) i;

            for (uint i = 0, j = 0; i < STATE_ARRAY_SIZE; i++)
            {
                j = (j + key[i % key.Length] + S[i]) & 255;
                this.swap(S, i, j);
            }
 
           index_i=0;
           index_j=0;
        }

        /* PRGA */
        private  byte rc4_output()
        {

            index_i = (index_i + 1) & 255;
            index_j = (index_j + S[index_i]) & 255;
            this.swap(S, index_i, index_j);
            return S[(S[index_i] + S[index_j]) & 255];
        }
        public string encode(string textToEncode,string key)
        {
            byte[] bTextArray = ASCIIEncoding.ASCII.GetBytes(textToEncode);
            byte[] bKeyArray = ASCIIEncoding.ASCII.GetBytes(key);
            
            String  encodedText ="";
            rc4_init(bKeyArray);

            //Se codifica el texto y se pasa a hexadecimal
            foreach (byte b in bTextArray)
            {
                encodedText += String.Format("{0:X2}", (b ^ rc4_output()));

            }
            return encodedText;
        }

        public string decode(string textToDecode, string key)
        {
            byte[] bDTextArray = new byte[textToDecode.Length / 2];
            char[] bDTmp = new char[textToDecode.Length];
            byte[] bDTextA = new byte[textToDecode.Length / 2];
            byte[] bKeyArray = ASCIIEncoding.ASCII.GetBytes(key);

            bDTmp = textToDecode.ToCharArray();

            rc4_init(bKeyArray);

            int index = 0;
            for (int i = 0; i < textToDecode.Length; i += 2)
            {
                bDTextArray[index] = (byte)(Convert.ToUInt16(bDTmp[i].ToString(), 16) << 4 | Convert.ToUInt16(bDTmp[i + 1].ToString(), 16));
                index++;
            }
            index = 0;
            foreach (byte b in bDTextArray)
            {
                bDTextA[index] = (byte)(b ^ rc4_output());
                index++;
            }

            return ASCIIEncoding.ASCII.GetString(bDTextA);
        }
        
    }

}


 
