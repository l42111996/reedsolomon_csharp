namespace fec
{
    public class InputOutputByteTableCodingLoop : CodingLoopBase
    {
        public override void codeSomeShards(byte[][] matrixRows, byte[][] inputs, int inputCount, byte[][] outputs,
            int outputCount,
            int offset, int byteCount)
        {
            byte[][] table = Galois.MULTIPLICATION_TABLE;
            {
                int iInput = 0;
                byte[] inputShard = inputs[iInput];
                for (int iOutput = 0; iOutput < outputCount; iOutput++)
                {
                    byte[] outputShard = outputs[iOutput];
                    byte[] matrixRow = matrixRows[iOutput];
                    byte[] multTableRow = table[matrixRow[iInput] ];
                    for (int iByte = offset; iByte < offset + byteCount; iByte++)
                    {
                        outputShard[iByte] = multTableRow[inputShard[iByte]];
                    }
                }
            }

            for (int iInput = 1; iInput < inputCount; iInput++)
            {
                byte[] inputShard = inputs[iInput];
                for (int iOutput = 0; iOutput < outputCount; iOutput++)
                {
                    byte[] outputShard = outputs[iOutput];
                    byte[] matrixRow = matrixRows[iOutput];
                    byte[] multTableRow = table[matrixRow[iInput]];
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

            // This is actually the code from OutputInputByteTableCodingLoop.
            // Using the loops from this class would require multiple temp
            // buffers.

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
                        tempBuffer[iByte] = multTableRow[inputShard[iByte] ];
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