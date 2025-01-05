public static class BitTricks
{
    public static bool GetBit(this uint value, int bitIndex)
    {
        return (value & (1u << bitIndex)) != 0;
    }

    public static uint ExtractFromRight(this uint value, int position, int length)
    {
        return (value >> position) & ((1u << length) - 1);
    }
}
