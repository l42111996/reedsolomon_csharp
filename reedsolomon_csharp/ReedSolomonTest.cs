using System;
using System.Collections.Generic;

namespace fec
{
    public class ReedSolomonTest
    {


    /**
     * Try encoding and decoding with a lot of shards.
     */
    public void testBigEncodeDecode() {
         Random random = new Random(0);
         for (int k = 0; k < 1000; k++)
         {
             int dataCount = 64;
             int parityCount = 64;
             int shardSize = 200;
             byte [] [] dataShards = new byte [dataCount] [];
             for (var j = 0; j < dataShards.Length; j++)
             {
                 var shard = dataShards[j] = new byte[shardSize];
                 for (int i = 0; i < shard.Length; i++) {
                     shard[i] = (byte) random.Next(256);
                 }
             }
             runEncodeDecode(dataCount, parityCount, dataShards);
         }

        Console.WriteLine("测试完成");
    }

    /**
     * Encodes a set of data shards, and then tries decoding
     * using all possible subsets of the encoded shards.
     *
     * Uses 5+5 coding, so there must be 5 input data shards.
     */
    private void runEncodeDecode(int dataCount, int parityCount, byte[][] dataShards) {

         int totalCount = dataCount + parityCount;
         int shardLength = dataShards[0].Length;

        // Make the list of data and parity shards.
//        assertEquals(dataCount, dataShards.length);
         int dataLength = dataShards[0].Length;
         byte [] [] allShards = new byte [totalCount] [];
        for (int i = 0; i < dataCount; i++) {
            byte[] temp = new byte[dataLength];
            Array.Copy(dataShards[i],0,temp,0,dataLength);
            allShards[i] = temp;
        }
        for (int i = dataCount; i < totalCount; i++) {
            allShards[i] = new byte [dataLength];
        }

        // Encode.
        ReedSolomon codec = ReedSolomon.create(dataCount, parityCount);
        codec.encodeParity(allShards, 0, dataLength);

        // Make a copy to decode with.
        byte [] [] testShards = new byte [totalCount] [];
        bool [] shardPresent = new bool [totalCount];
        for (int i = 0; i < totalCount; i++) {
            byte[] temp = new byte[shardLength];
            Array.Copy(allShards[i],0,temp,0,shardLength);
            testShards[i] = temp;
            shardPresent[i] = true;
        }

        // Decode with 0, 1, ..., 5 shards missing.
        for (int numberMissing = 0; numberMissing < parityCount + 1; numberMissing++) {
            tryAllSubsetsMissing(codec, allShards, testShards, shardPresent, numberMissing);
        }
    }

    private void tryAllSubsetsMissing(ReedSolomon codec,
        byte [] [] allShards, byte [] [] testShards,
                                      bool [] shardPresent, int numberMissing) {
         int shardLength = allShards[0].Length;
        List<int []> subsets = allSubsets(numberMissing, 0, 10);
        foreach (var subset in subsets) {
            // Get rid of the shards specified by this subset.
            foreach (var missingShard in subset)
            {
                clearBytes(testShards[missingShard]);
                shardPresent[missingShard] = false;
            }

            // Reconstruct the missing shards
            codec.decodeMissing(testShards, shardPresent, 0, shardLength);

            // Check the results.  After checking, the contents of testShards
            // is ready for the next test, the next time through the loop.
            checkShards(allShards, testShards);

            // Put the "present" flags back
            for (int i = 0; i < codec.getTotalShardCount(); i++) {
                shardPresent[i] = true;
            }
        }
    }





    private void assertTrue(bool isParityCorrect)
    {
        throw new NotImplementedException();
    }

    private void assertFalse(bool isParityCorrect)
    {
        throw new NotImplementedException();
    }

    private void clearBytes(byte [] data) {
        for (int i = 0; i < data.Length; i++) {
            data[i] = 0;
        }
    }

    private void checkShards(byte[][] expectedShards, byte[][] actualShards) {
        assertEquals(expectedShards.Length, actualShards.Length);
        for (int i = 0; i < expectedShards.Length; i++) {
            assertArrayEquals(expectedShards[i], actualShards[i]);
        }
    }

    private void assertArrayEquals(byte[] expectedShard, byte[] actualShard)
    {

        var len1 = expectedShard.Length;
        var len2 = actualShard.Length;
        if (len1 != len2)
        {
            throw new NotImplementedException();
        }
        for (var i = 0; i < len1; i++)
        {
            if (expectedShard[i] != actualShard[i])
            {
                throw new NotImplementedException();
            }
        }
    }

    private void assertEquals(int expectedShardsLength, int actualShardsLength)
    {
        if (expectedShardsLength != actualShardsLength)
        {
            throw new NotImplementedException();
        }
    }


    /**
     * Returns a list of arrays with all possible sets of
     * unique values where (min <= n < max).
     *
     * This is NOT EFFICIENT, because it allocates lots of
     * temporary arrays, but it's OK for these tests.
     *
     * To avoid duplicates that are in a different order,
     * each subset is generated with elements in increasing
     * order.
     *
     * Given (n=2, min=1, max=4), returns:
     *    [1, 2]
     *    [1, 3]
     *    [1, 4]
     *    [2, 3]
     *    [2, 4]
     *    [3, 4]
     */
    private List<int []> allSubsets(int n, int min, int max) {
        List<int []> result = new List<int[]>();
        if (n == 0) {
            result.Add(new int [0]);
        }
        else {
            for (int i = min; i < max - n; i++) {
                int [] prefix = { i };
                foreach (var suffix in allSubsets(n - 1, i + 1, max))
                {
                    result.Add(appendIntArrays(prefix, suffix));
                }
            }
        }
        return result;
    }

    private int [] appendIntArrays(int [] a, int [] b) {
        int [] result = new int[a.Length + b.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }
    }
}