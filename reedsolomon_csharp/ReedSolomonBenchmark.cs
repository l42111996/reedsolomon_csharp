using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace fec
{
    public class ReedSolomonBenchmark
    {
        public static readonly CodingLoop[] ALL_CODING_LOOPS =
            new CodingLoop[] {
                new InputOutputByteTableCodingLoop(),
                new OutputInputByteTableCodingLoop(),
            };


        private const int DATA_COUNT = 17;

        private const int PARITY_COUNT = 3;

        private const int TOTAL_COUNT = DATA_COUNT + PARITY_COUNT;

        private const int BUFFER_SIZE = 200 * 1000;

        private const int PROCESSOR_CACHE_SIZE = 10 * 1024 * 1024;

        private const int TWICE_PROCESSOR_CACHE_SIZE = 2 * PROCESSOR_CACHE_SIZE;

        private const int NUMBER_OF_BUFFER_SETS = TWICE_PROCESSOR_CACHE_SIZE / DATA_COUNT / BUFFER_SIZE + 1;



        private const long MEASUREMENT_DURATION = 2 * 1000;



        private static readonly Random random = new Random();



        private int nextBuffer = 0;


        public void run()
        {
            Console.WriteLine("preparing...");
            BufferSet[] bufferSets = new BufferSet [NUMBER_OF_BUFFER_SETS];
            for (int iBufferSet = 0; iBufferSet < NUMBER_OF_BUFFER_SETS; iBufferSet++)
            {
                bufferSets[iBufferSet] = new BufferSet();
            }

            byte[] tempBuffer = new byte [BUFFER_SIZE];

            List<String> summaryLines = new List<String>();
            StringBuilder csv = new StringBuilder();
            csv.Append("Outer,Middle,Inner,Multiply,Encode,Check\n");
            foreach (var codingLoop in ALL_CODING_LOOPS)
            {
                Measurement encodeAverage = new Measurement();
                {
                    String testName = codingLoop.GetType().Name + " encodeParity";


                    Console.WriteLine("\nTEST: " + testName);
                    ReedSolomon codec = new ReedSolomon(DATA_COUNT, PARITY_COUNT, codingLoop);
                    Console.WriteLine("    warm up...");
                    doOneEncodeMeasurement(codec, bufferSets);
                    doOneEncodeMeasurement(codec, bufferSets);
                    Console.WriteLine("    testing...");
                    for (int iMeasurement = 0; iMeasurement < 10; iMeasurement++)
                    {
                        encodeAverage.add(doOneEncodeMeasurement(codec, bufferSets));
                    }

                    Console.WriteLine("AVERAGE: {0}", encodeAverage);
                    summaryLines.Add(testName+" "+encodeAverage);
                }
                // The encoding test should have filled all of the buffers with
                // correct parity, so we can benchmark parity checking.
                Measurement checkAverage = new Measurement();
                {
                    String testName = codingLoop.GetType().Name + " isParityCorrect";
                    Console.WriteLine("\nTEST: " + testName);
                    ReedSolomon codec = new ReedSolomon(DATA_COUNT, PARITY_COUNT, codingLoop);
                    Console.WriteLine("    warm up...");
                    doOneEncodeMeasurement(codec, bufferSets);
                    doOneEncodeMeasurement(codec, bufferSets);
                    Console.WriteLine("    testing...");
                    for (int iMeasurement = 0; iMeasurement < 10; iMeasurement++)
                    {
                        checkAverage.add(doOneCheckMeasurement(codec, bufferSets, tempBuffer));
                    }

                    Console.WriteLine("AVERAGE: {0}", checkAverage);
                    summaryLines.Add(testName+" "+checkAverage);
                }
                csv.Append(codingLoopNameToCsvPrefix(codingLoop.GetType().Name));
                csv.Append((int)encodeAverage.getRate());
                csv.Append(",");
                csv.Append((int)checkAverage.getRate());
                csv.Append("\n");
            }

            Console.WriteLine("\n");
            Console.WriteLine(csv.ToString());

            Console.WriteLine("\nSummary:\n");
            foreach (var line in summaryLines)
            { Console.WriteLine(line);
            }
        }

        private Measurement doOneEncodeMeasurement(ReedSolomon codec, BufferSet[] bufferSets)
        {
            long passesCompleted = 0;
            long bytesEncoded = 0;
            long encodingTime = 0;
            while (encodingTime < MEASUREMENT_DURATION)
            {
                BufferSet bufferSet = bufferSets[nextBuffer];
                nextBuffer = (nextBuffer + 1) % bufferSets.Length;
                byte[][] shards = bufferSet.buffers;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                codec.encodeParity(shards, 0, BUFFER_SIZE);
                stopwatch.Stop();
                long stop = stopwatch.ElapsedMilliseconds;
                TimeSpan timespan = stopwatch.Elapsed; 
                encodingTime += (long)timespan.TotalMilliseconds;
                bytesEncoded += BUFFER_SIZE * DATA_COUNT;
                passesCompleted += 1;
            }

            double seconds = ((double) encodingTime) / 1000.0;
            double megabytes = ((double) bytesEncoded) / 1000000.0;
            Measurement result = new Measurement(megabytes, seconds);
            Console.WriteLine("        {0} passes, {1}", passesCompleted, result.ToString());
            return result;
        }

        private Measurement doOneCheckMeasurement(ReedSolomon codec, BufferSet[] bufferSets, byte[] tempBuffer)
        {
            long passesCompleted = 0;
            long bytesChecked = 0;
            long checkingTime = 0;
            while (checkingTime < MEASUREMENT_DURATION)
            {
                BufferSet bufferSet = bufferSets[nextBuffer];
                nextBuffer = (nextBuffer + 1) % bufferSets.Length;
                byte[][] shards = bufferSet.buffers;

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (!codec.isParityCorrect(shards, 0, BUFFER_SIZE, tempBuffer))
                {
                    // if the parity is not correct, it will throw off the
                    // benchmarking because it may return early.
                    throw new Exception("parity not correct");
                }

                stopwatch.Stop(); //  停止监视
                TimeSpan timespan = stopwatch.Elapsed; 
                checkingTime += (long)timespan.TotalMilliseconds;
                bytesChecked += BUFFER_SIZE * DATA_COUNT;
                passesCompleted += 1;
            }

            double seconds = ((double) checkingTime) / 1000.0;
            double megabytes = ((double) bytesChecked) / 1000000.0;
            Measurement result = new Measurement(megabytes, seconds);
            Console.WriteLine("        {0} passes, {1}", passesCompleted, result);
            return result;
        }

        /**
         * Converts a name like "OutputByteInputTableCodingLoop" to
         * "output,byte,input,table,".
         */
        private static string codingLoopNameToCsvPrefix(string className)
        {
            List<string> names = splitCamelCase(className);
            return
                names[0] + "," +
                names[1] + "," +
                names[2] + "," +
                names[3] + ",";
        }

        /**
         * Converts a name like "OutputByteInputTableCodingLoop" to a List of
         * words: { "output", "byte", "input", "table", "coding", "loop" }
         */
        private static List<string> splitCamelCase(string className)
        {
            string remaining = className;
            List<string> result = new List<string>();
            while (remaining.Length!=0)
            {
                bool found = false;
                for (int i = 1; i < remaining.Length; i++)
                {
                    if (remaining[i] >= 'A' && remaining[i] <= 'Z')
                    {
                        result.Add(remaining.Substring(0, i));
                        remaining = remaining.Substring(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    result.Add(remaining);
                    remaining = "";
                }
            }

            return result;
        }


        private class BufferSet
        {
            public readonly byte[][] buffers;

            public readonly byte[] bigBuffer;

            public BufferSet()
            {
                buffers = new byte [TOTAL_COUNT][];
                for (int iBuffer = 0; iBuffer < TOTAL_COUNT; iBuffer++)
                {
                    byte[] buffer = new byte[BUFFER_SIZE];
                    buffers[iBuffer] = buffer;
                    for (int iByte = 0; iByte < BUFFER_SIZE; iByte++)
                    {
                        buffer[iByte] = (byte) random.Next(256);
                    }
                }

                bigBuffer = new byte [TOTAL_COUNT * BUFFER_SIZE];
                for (int i = 0; i < TOTAL_COUNT * BUFFER_SIZE; i++)
                {
                    bigBuffer[i] = (byte) random.Next(256);
                }
            }
        }

        private class Measurement
        {
            private double megabytes;
            private double seconds;

            public Measurement()
            {
                this.megabytes = 0.0;
                this.seconds = 0.0;
            }

            public Measurement(double megabytes, double seconds)
            {
                this.megabytes = megabytes;
                this.seconds = seconds;
            }

            public void add(Measurement other)
            {
                megabytes += other.megabytes;
                seconds += other.seconds;
            }

            public double getRate()
            {
                return megabytes / seconds;
            }

            public override string ToString()
            {
                return string.Format((int)getRate()+"MB/s" );
            }
        }
    }
}