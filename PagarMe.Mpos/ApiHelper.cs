﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.OpenSsl;
using Newtonsoft.Json;

namespace PagarMe.Mpos
{
    public static class ApiHelper
    {
        public static string ApiEndpoint { get; set; }

        static ApiHelper()
        {
            ApiEndpoint = "https://api.pagar.me/1";
			//ApiEndpoint = "http://192.168.64.2:3000";
        }

        static HttpWebRequest CreateRequest(string method, string path, string auth)
        {
            var request = WebRequest.Create(ApiEndpoint + path);

            request.Method = method;

            if (auth != null)
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(auth + ":x")));

            return (HttpWebRequest)request;
        }

        static async Task<Tuple<string, string>> GetCardHashKey(string encryptionKey)
        {
			var response = (HttpWebResponse)(await CreateRequest("GET", "/transactions/card_hash_key", encryptionKey).GetResponseAsync());

            var json = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            var result = JsonConvert.DeserializeObject<dynamic>(json);

            return new Tuple<string, string>(result.id.ToString(), result.public_key.ToString());
        }

        static byte[] Combine(byte[][] arrays)
        {
            int offset = 0;
            byte[] result = new byte[arrays.Sum(a => a.Length)];

            foreach (byte[] array in arrays) {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }

        static byte[] Encrypt(byte[] data, AsymmetricKeyParameter key)
        {
	    List<byte[]> output = new List<byte[]>();
            Pkcs1Encoding engine = new Pkcs1Encoding(new RsaEngine());

            engine.Init(true, key);

            int blockSize = engine.GetInputBlockSize();

            for (int chunkPosition = 0; chunkPosition < data.Length; chunkPosition += blockSize)
            {
                int chunkSize = Math.Min(blockSize, data.Length - chunkPosition);
                output.Add(engine.ProcessBlock(data, chunkPosition, chunkSize));
            }

            return Combine(output.ToArray());
        }

        static string EncryptWith(string id, string publicKey, string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            using (var reader = new StringReader(publicKey))
            {
                var pemReader = new PemReader(reader);
                var key = (AsymmetricKeyParameter)pemReader.ReadObject();

                return id + "_" + Convert.ToBase64String(Encrypt(bytes, key));
            }
        }

        public static async Task<string> CreateCardHash(string encryptionKey, string data)
        {
            Tuple<string, string> hashParameters = await GetCardHashKey(encryptionKey);

            return EncryptWith(hashParameters.Item1, hashParameters.Item2, data);
        }

        public static async Task<string> GetTerminalTables(string encryptionKey, string checksum, int[] dukptKeys)
        {
			var request = CreateRequest("GET", "/terminal/updates?checksum=" + WebUtility.UrlEncode(checksum) + "&dukpt_keys=[" + String.Join(",", dukptKeys) + "]", encryptionKey);
			try {
				var response = (HttpWebResponse)(await request.GetResponseAsync());
				return new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
			}
			catch (WebException ex) {
				if (ex.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotModified)
					return "";

				throw;
			}
        }
    }
}

