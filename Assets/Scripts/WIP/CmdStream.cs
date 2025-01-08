using System;
using System.IO;
using System.Linq;
using System.Text;

public class CmdStream
{
    private static byte[] statbuff;
    private byte[] mBuffer;
    private Stream mStream;
    private int mTotalStreamSize;
    private int mCurrentPosition;
    private int mBufferUsed;
    private int mBufferOffset;
    private char[] mCurrentToken = new char[8192]; // Reasonable buffer size

    public CmdStream() { }

    public CmdStream(Stream stream)
    {
        Init(stream);
    }

    public void Init(Stream stream)
    {
        mBuffer = null;

        if (statbuff == null)
        {
            statbuff = new byte[0x8000];
        }

        Array.Clear(statbuff, 0, 0x8000);

        mStream = stream;
        mTotalStreamSize = (int)mStream.Length;
        mCurrentPosition = 0;
        mBufferUsed = 0;
        mBufferOffset = 0;
        mBuffer = statbuff;
        FillBuffer(true);
    }

    private void FillBuffer(bool force)
    {
        if (force || mCurrentPosition - mBufferOffset >= 0x4000)
        {
            if (mBufferUsed > 0)
            {
                Buffer.BlockCopy(mBuffer, mBufferOffset + 0x4000, mBuffer, mBufferOffset, 0x4000);
                mBuffer = mBuffer.Take(mBuffer.Length - 0x4000).ToArray();
                mBufferOffset += 0x4000;
            }

            int size = mBufferUsed > 0 ? 0x4000 : 0x8000;
            if (mTotalStreamSize - mBufferUsed < size)
            {
                size = mTotalStreamSize - mBufferUsed;
            }

            mStream.Read(mBuffer, mBufferUsed, size);
            mBufferUsed += size;
        }
    }

    private bool WhiteSpace(char toCheck)
    {
        return toCheck == '\0'
            || toCheck == '\r'
            || toCheck == '\n'
            || toCheck == '\t'
            || toCheck == ' '
            || toCheck == ',';
    }

    public bool EndOfCmds()
    {
        FillBuffer(false);

        while (mCurrentPosition < mTotalStreamSize && WhiteSpace((char)mBuffer[mCurrentPosition]))
        {
            mCurrentPosition++;
        }

        if (mCurrentPosition < mTotalStreamSize)
        {
            if (mBuffer[mCurrentPosition] == 0xFF)
            {
                return true;
            }
            return false;
        }

        return true;
    }

    private bool LineIsComment()
    {
        return mBuffer[mCurrentPosition] == (byte)'#'
            || (
                mBuffer[mCurrentPosition] == (byte)'/' && mBuffer[mCurrentPosition + 1] == (byte)'/'
            );
    }

    private void CopyToToken(int length)
    {
        for (int i = 0; i < length; i++)
        {
            char currCharOfToken = (char)mBuffer[mCurrentPosition + i];
            if (currCharOfToken == '\t')
            {
                currCharOfToken = ' ';
            }
            mCurrentToken[i] = currCharOfToken;
        }
        mCurrentToken[length] = '\0';
    }

    public string SkipLine()
    {
        FillBuffer(false);

        int currentPos = mCurrentPosition;
        while (
            currentPos < mTotalStreamSize
            && (char)mBuffer[currentPos] != '\n'
            && (char)mBuffer[currentPos] != '\r'
        )
        {
            currentPos++;
        }

        CopyToToken(currentPos - mCurrentPosition);

        while ((char)mBuffer[currentPos] == '\n' || (char)mBuffer[currentPos] == '\r')
        {
            currentPos++;
        }

        mCurrentPosition = currentPos;

        return new string(mCurrentToken.TakeWhile(c => c != '\0').ToArray());
    }

    public string GetToken(bool skipComments = true)
    {
        FillBuffer(false);

        if (skipComments)
        {
            while (LineIsComment())
            {
                SkipLine();
            }
        }

        int currChar = mCurrentPosition;
        bool tokenInParenthesis = false;

        if ((char)mBuffer[currChar] == '"' || (char)mBuffer[currChar] == '\'')
        {
            tokenInParenthesis = true;
            ++currChar;
            ++mCurrentPosition;
        }

        while (
            tokenInParenthesis
                ? ((char)mBuffer[currChar] != '"' && (char)mBuffer[currChar] != '\'')
                : !WhiteSpace((char)mBuffer[currChar])
        )
        {
            currChar++;
        }

        CopyToToken(currChar - mCurrentPosition);

        if (tokenInParenthesis)
        {
            mCurrentToken[currChar - mCurrentPosition] = '\0';
            currChar++;
        }

        while (currChar < mTotalStreamSize && WhiteSpace((char)mBuffer[currChar]))
        {
            currChar++;
        }

        mCurrentPosition = currChar;
        return new string(mCurrentToken.TakeWhile(c => c != '\0').ToArray());
    }

    public char NextChar()
    {
        return (char)mBuffer[mCurrentPosition];
    }

    public bool IsToken(string str)
    {
        var currentTokenStr = new string(mCurrentToken.TakeWhile(c => c != '\0').ToArray());

        if (string.IsNullOrEmpty(currentTokenStr) || currentTokenStr.Length != str.Length)
        {
            return false;
        }

        return currentTokenStr == str;
    }

    public bool EndOfSection()
    {
        FillBuffer(false);
        return (char)mBuffer[mCurrentPosition] == '}';
    }
}
