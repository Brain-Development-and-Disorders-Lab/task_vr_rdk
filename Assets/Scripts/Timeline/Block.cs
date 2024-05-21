namespace Timeline
{
    // Define the types of blocks that occur during the experiment timeline
    public enum BlockType
    {
        Setup = 1,
        Instructions_Introduction = 2,
        Training_Trials_Binocular = 3,
        Main_Trials_Binocular = 4,
        Training_Trials_Monocular = 5,
        Main_Trials_Monocular = 6,
        Training_Trials_Lateralized = 7,
        Main_Trials_Lateralized = 8,
        PostMain = 9,
    };

    /// <summary>
    /// Utility class to store a "Block" classification of a trial type, where multiple
    /// trials are involved
    /// </summary>
    class Block
    {
        private BlockType blockType;
        private int blockLength = 0;

        public Block(BlockType type, int length)
        {
            blockType = type;
            blockLength = length;
        }

        public int GetLength()
        {
            return blockLength;
        }

        public BlockType GetBlockType()
        {
            return blockType;
        }
    }
}
