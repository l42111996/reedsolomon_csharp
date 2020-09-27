/**
 * One specific ordering/nesting of the coding loops.
 *
 * Copyright 2015, Backblaze, Inc.  All rights reserved.
 */

namespace fec
{
    public class OutputInputByteTableCodingLoop : CodingLoopBase
    {
        public override void codeSomeShards(
            byte[][] matrixRows,
            byte[][] inputs, int inputCount,
            byte[][] outputs, int outputCount,
            int offset, int byteCount)
        {
            byte[][] table = Galois.MULTIPLICATION_TABLE;
            for (int iOutput = 0; iOutput < outputCount; iOutput++)
            {
                byte[] outputShard = outputs[iOutput];
                byte[] matrixRow = matrixRows[iOutput];
                {
                    int iInput = 0;
                    byte[] inputShard = inputs[iInput];
                    byte[] multTableRow = table[matrixRow[iInput] ];
                    for (int iByte = offset; iByte < offset + byteCount; iByte++)
                    {
                        outputShard[iByte] = multTableRow[inputShard[iByte]];
                    }
                }
                for (int iInput = 1; iInput < inputCount; iInput++)
                {
                    byte[] inputShard = inputs[iInput];
                    byte[] multTableRow = table[matrixRow[iInput] & 0xFF];
                    for (int iByte = offset; iByte < offset + byteCount; iByte++)
                    {
                        outputShard[iByte] ^= multTableRow[inputShard[iByte]];
                    }
                }
            }
        }


        public override bool checkSomeShards(
            byte[][] matrixRows,
            byte[][] inputs, int inputCount,
            byte[][] toCheck, int checkCount,
            int offset, int byteCount,
            byte[] tempBuffer)
        {
            if (tempBuffer == null)
            {
                return base.checkSomeShards(matrixRows, inputs, inputCount, toCheck, checkCount, offset, byteCount,
                    null);
            }

            byte[][] table = Galois.MULTIPLICATION_TABLE;
            for (int iOutput = 0; iOutput < checkCount; iOutput++)
            {
                byte[] outputShard = toCheck[iOutput];
                byte[] matrixRow = matrixRows[iOutput];
                {
                    int iInput = 0;
                    byte[] inputShard = inputs[iInput];
                    byte[] multTableRow = table[matrixRow[iInput]];
                    for (int iByte = offset; iByte < offset + byteCount; iByte++)
                    {
                        tempBuffer[iByte] = multTableRow[inputShard[iByte]];
                    }
                }
                for (int iInput = 1; iInput < inputCount; iInput++)
                {
                    byte[] inputShard = inputs[iInput];
                    byte[] multTableRow = table[matrixRow[iInput]];
                    for (int iByte = offset; iByte < offset + byteCount; iByte++)
                    {
                        tempBuffer[iByte] ^= multTableRow[inputShard[iByte]];
                    }
                }

                for (int iByte = offset; iByte < offset + byteCount; iByte++)
                {
                    if (tempBuffer[iByte] != outputShard[iByte])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}