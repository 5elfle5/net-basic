using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MBasic
{
    public class Tokenizer
    {
        public Dictionary<string, Token> keywordTokens = new Dictionary<string, Token>();
        public Dictionary<string, Token> signleCharTokens = new Dictionary<string, Token>();

        public string progText = "";
        public int textIndex = 0;
        public const int Max_Num_Len = 5;
        public int current = 0;
        public int next = 0;
        public Token currentToken { get; private set; }
        public int LineNumber { get; private set; }
        public int VariableNumber
        {
            get
            {
                return (int)progText[current] - (int)'a';
            }
        }
        public Tokenizer(string programText)
        {
            progText = programText;
            LineNumber = 0;
            Init();
        }
        public void Init()
        {
            keywordTokens["let"] = Token.LET;
            keywordTokens["print"] = Token.PRINT;
            keywordTokens["if"] = Token.IF;            
            keywordTokens["then"] = Token.THEN;
            keywordTokens["else"] = Token.ELSE;
            keywordTokens["for"] = Token.FOR;
            keywordTokens["to"] = Token.TO;
            keywordTokens["next"] = Token.NEXT;
            keywordTokens["goto"] = Token.GOTO;
            keywordTokens["gosub"] = Token.GOSUB;
            keywordTokens["return"] = Token.RETURN;
            keywordTokens["call"] = Token.CALL;
            keywordTokens["end"] = Token.END;
            
            signleCharTokens["\n"] = Token.CR;
            signleCharTokens[","] = Token.COMMA;
            signleCharTokens[";"] = Token.SEMICOLON;
            signleCharTokens["+"] = Token.PLUS;
            signleCharTokens["-"] = Token.MINUS;
            signleCharTokens["&"] = Token.AND;
            signleCharTokens["|"] = Token.OR;
            signleCharTokens["*"] = Token.ASTR;
            signleCharTokens["/"] = Token.SLASH;
            signleCharTokens["%"] = Token.MOD;
            signleCharTokens["("] = Token.LEFTPAREN;
            signleCharTokens[")"] = Token.RIGHTPAREN;
            signleCharTokens["<"] = Token.LT;
            signleCharTokens[">"] = Token.GT;
            signleCharTokens["="] = Token.EQ;

        }
        public Token GetNextToken()
        {
            if (textIndex >= progText.Length)
            {
                return Token.ENDOFINPUT;
            }
            if (char.IsDigit(progText[current]))
            {
                for (int i = 0; i < Max_Num_Len; i++)
                {
                    if (!char.IsDigit(progText[current+i]))
                    {
                        if (i > 0)
                        {
                            next = current + i;
                            return Token.NUMBER;
                        }
                    }
                }
                //too long number
                return Token.ERROR;
            }
            else if (signleCharTokens[progText[current].ToString()] != null)
            {
                next = current + 1;
                if (progText[current] == '\n')
                {
                    LineNumber++;
                }
                return signleCharTokens[progText[current].ToString()];
            }
            else if (progText[current] == '"')
            {
                next = current;
                do
                {
                    next++;
                } while (progText[next] != '"');
                next++;
                return Token.STRING;
            }
            else
            {
                foreach (var keyToken in keywordTokens)
                {
                    int len = keyToken.Key.Length;
                    string keyword = progText.Substring(current, len);
                    if (keyword.Equals(keyToken.Key))
                    {
                        return keyToken.Value;
                    }
                }
            }
            if (char.IsLetter(progText[current]))
            {
                return Token.VARIABLE;
            }
            return Token.ERROR;
        }
        public bool TokenizerFinished()
        {
            if (current >= progText.Length)
            {
                return true;
            }
            return false;
        }
        public void GoToNextToken()
        {
            if (TokenizerFinished())
            {
                return;
            }
            current = next;
            while (progText[current] == ' ')
            {
                current++;
            }
            currentToken = GetNextToken();
        }

        public string GetString()
        {
            if (currentToken != Token.STRING)
            {
                return null;
            }
            int start = progText.IndexOf('"', current);
            int end = progText.IndexOf('"', start + 1);
            string res = progText.Substring(start, end - start);// !!!!!!!!!!
            return res;
        }
        
        public int GetNumber()
        {
            if (currentToken != Token.NUMBER)
            {
                throw new Exception("i wanted a number and din't get one");
            }
            for (int i = current; i < progText.Length; i++)
            {
                if (!char.IsDigit(progText[i]))
                {
                    if (i - current > 0)
                    {
                        return int.Parse(progText.Substring(current, i - current));
                    }
                }
            }
            throw new Exception("i wanted a number and din't get one");
        }
    }
    enum Token {
          ERROR,
          ENDOFINPUT,
          NUMBER,
          STRING,
          VARIABLE,
          LET,
          PRINT,
          IF,
          THEN,
          ELSE,
          FOR,
          TO,
          NEXT,
          GOTO,
          GOSUB,
          RETURN,
          CALL,
          END,
          COMMA,
          SEMICOLON,
          PLUS,
          MINUS,
          AND,
          OR,
          ASTR,
          SLASH,
          MOD,
          LEFTPAREN,
          RIGHTPAREN,
          LT,
          GT,
          EQ,
          CR,
    }
}
