namespace ParseFive.Tokenizer
{
    enum TokenType
    {
        CHARACTER_TOKEN = 1,
        NULL_CHARACTER_TOKEN,
        WHITESPACE_CHARACTER_TOKEN,
        START_TAG_TOKEN,
        END_TAG_TOKEN,
        COMMENT_TOKEN,
        DOCTYPE_TOKEN,
        EOF_TOKEN,
        HIBERNATION_TOKEN,
    }
}