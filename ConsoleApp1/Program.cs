using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace InvestCloudDemo
{
    class Program
    {
        static void Main(string[] args)
        {

            int matrixSize = 1000;
            bool verbose = false;
            int[][] AmatrixRow;
            int[][] BmatrixCol;
            string apiUri = "https://recruitment-test.investcloud.com/api/";


            var watchOverall = System.Diagnostics.Stopwatch.StartNew();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // set up initialization GET Request
            var JSONObj  = InvestCloudAPI.Get(apiUri + "numbers/init/" + matrixSize);
            InitResponse initResponse = JsonConvert.DeserializeObject<InitResponse>(JSONObj);
            watch.Stop();
            Console.WriteLine($"init value: {initResponse.value}");
            Console.WriteLine($"init time: { watch.ElapsedMilliseconds} ms");

            // get matrix data
            watch = System.Diagnostics.Stopwatch.StartNew();
            var getDataAsync = GetDataAsync(matrixSize, apiUri);
            AmatrixRow = getDataAsync.Item1;
            BmatrixCol = getDataAsync.Item2;
            if (verbose)
            {
                for (int i = 0; i < matrixSize; i++)
                {
                    if (i == 0)
                    {
                        Console.WriteLine();
                        Console.Write("[");
                    }

                    for (int j = 0; j < matrixSize; j++)
                    {
                        Console.Write(AmatrixRow[i][j] + " ");
                    }

                    if (i == matrixSize - 1)
                    {
                        Console.Write("]");
                    }
                    else
                    {
                        Console.WriteLine();
                    }

                }

                for (int i = 0; i < matrixSize; i++)
                {
                    if (i == 0)
                    {
                        Console.WriteLine();
                        Console.Write("[");
                    }

                    for (int j = 0; j < matrixSize; j++)
                    {
                        Console.Write(BmatrixCol[j][i] + " ");
                    }

                    if (i == matrixSize - 1)
                    {
                        Console.Write("]");
                    }
                    else
                    {
                        Console.WriteLine();
                    }

                }
            }
            watch.Stop();
            Console.WriteLine($"download time: {watch.ElapsedMilliseconds} ms");

            // perform multiplication
            watch = System.Diagnostics.Stopwatch.StartNew();
            string AxB = MultiplyAndSerialize(AmatrixRow, BmatrixCol, matrixSize);
            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"AxBmatrix: {AxB}");
            }

            // generate hash
            string md5 = CalculateMD5Hash(AxB);
            watch.Stop();
            Console.WriteLine($"hash: {md5}");
            Console.WriteLine($"multiplication and hash time: {watch.ElapsedMilliseconds} ms");
            
            // validate response with server
            JSONObj = InvestCloudAPI.Post(apiUri + "numbers/validate", md5, "text/json;charset=utf-8");
            ValidateResponse validateResponse = JsonConvert.DeserializeObject<ValidateResponse>(JSONObj);
            Console.WriteLine($"result: {validateResponse.value}");
            watchOverall.Stop();
            Console.WriteLine($"total time: {watchOverall.ElapsedMilliseconds} ms");
            Console.WriteLine("press any key to exit");
            Console.ReadLine();
        }

        public static string MultiplyAndSerialize(int[][] AmatrixRow, int[][] BmatrixCol, int matrixSize)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < matrixSize; i++)
            {
                for (int j = 0; j < matrixSize; j++)
                {
                    // get dot product of A (row) * B (col)
                    result.Append(DotProduct(AmatrixRow[i], BmatrixCol[j], matrixSize));
                }
            }
            return result.ToString();
        }

        public static string DotProduct(int[] ARow, int[] BCol, int matrixSize)
        {
            int sum = 0;
            for (int i = 0; i < matrixSize; i++)
            {
                sum += ARow[i] * BCol[i];
            }

            return sum.ToString();
        }

        public static string CalculateMD5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to decimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString());
                }
                return sb.ToString();
            }
        }


        public static Tuple<int[][],int[][]> GetDataAsync(int matrixSize, string apiUri)
        {
            int[][] AmatrixRow = new int[matrixSize][];
            int[][] BmatrixCol = new int[matrixSize][];

            // setup an async task list to loop thru completed requests
            List<Task> TaskList = new List<Task>();
            
            // download all data at once
            for (int i = 0; i < matrixSize; i++)
            {
                // make requests to row and column data
                var getMatrixARowTask = InvestCloudAPI.GetAsync(apiUri + "numbers/A/row/", i, true);
                TaskList.Add(getMatrixARowTask);

                // matrix B are stored as columns because it makes the AxB multiply matrix easier
                var getMatrixBColTask = InvestCloudAPI.GetAsync(apiUri + "numbers/B/col/", i, false);
                TaskList.Add(getMatrixBColTask);
                
            }

            // wait for downloads to finish
            Task.WaitAll(TaskList.ToArray());

            // loop thru tasks and store result in their respective matrices
            foreach (Task<Tuple<string,int,bool>> t in TaskList)
            {
                if (t.Result.Item3)
                {
                    string resultMatrixARow = t.Result.Item1;
                    DataResponse resultMatrixAResponse = JsonConvert.DeserializeObject<DataResponse>(resultMatrixARow);
                    AmatrixRow[t.Result.Item2] = resultMatrixAResponse.value;
                }
                else
                {
                    string resultMatrixBCol = t.Result.Item1;
                    DataResponse resultMatrixBResponse = JsonConvert.DeserializeObject<DataResponse>(resultMatrixBCol);
                    BmatrixCol[t.Result.Item2] = resultMatrixBResponse.value;
                }
            }

            return new Tuple<int[][], int[][]>(AmatrixRow, BmatrixCol);
        }


        

        

        

        

        

    }


}
