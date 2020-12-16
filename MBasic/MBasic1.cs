using System;
using System.Collections.Generic;
using System.Text;

namespace MBasic
{
    class MBasic1
    {
        private static Tokenizer tokenizer;
        private static const int Max_Var_Count = 26;
        private static int[] Variables = new int[Max_Var_Count];
        private static string progText;
        private static const int Max_Gosub_Stack_Depth = 10;
        private static int[] gosubStack = new int[Max_Gosub_Stack_Depth];
        private static int gosubIndex = 0;
        private static const int Max_For_Stack_Depth = 4;
        private static ForState[] forStack = new ForState[Max_For_Stack_Depth];
        private static int forStackIndex = 0;
        private static bool Ended = false;
        public static void Run(string programText)
        {
            progText = programText;
            tokenizer = new Tokenizer(progText);
            while (tokenizer.currentToken != Token.END)
            {
                Statement();
            }
        }
        public static void Statement()
        {
            Token token = tokenizer.GetNextToken();
            switch (token)
            {
                case Token.PRINT:
                    PrintStatement();
                    break;
                case Token.IF:
                    IfStatement();
                    break;
                case Token.GOTO:
                    GotoStatement();
                    break;
                case Token.GOSUB:
                    GosubStatement();
                    break;
                case Token.RETURN:
                    ReturnStatement();
                    break;
                case Token.FOR:
                    ForStatement();
                    break;
                case Token.NEXT:
                    NextStatement();
                    break;
                case Token.END:
                    EndStatement();
                    break;
                case Token.LET:
                    //Accept(Token.LET);
                    //continue;
                    /* fall through */
                case Token.VARIABLE:
                    LetStatement();
                    break;
                default:
                    throw new Exception("Statement not implemented");
            }

        }
   
        public static void PrintStatement()
        {
            Accept(Token.PRINT);
                //print loop
            do
            {
                if (tokenizer.currentToken == Token.STRING)
                {
                    string str = tokenizer.GetString();
                    Console.Write(str);
                    tokenizer.GoToNextToken();
                }
                else if (tokenizer.currentToken == Token.COMMA)
                {
                    Console.Write(" ");
                    tokenizer.GoToNextToken();
                }
                else if (tokenizer.currentToken == Token.SEMICOLON)
                {
                    tokenizer.GoToNextToken();
                }
                else if (tokenizer.currentToken == Token.VARIABLE || 
                    tokenizer.currentToken == Token.NUMBER)
                {
                    Console.WriteLine();
                }

            } while (tokenizer.currentToken != Token.CR && 
                tokenizer.currentToken != Token.ENDOFINPUT);
            Console.WriteLine();
            tokenizer.GoToNextToken();
        }

        public static void IfStatement()
        {
            bool r = false;
            Accept(Token.IF);
            r = Relation();
            //if statement: relation r
            Accept(Token.THEN);
            if (r)
            {
                Statement();
            }
            else
            {
                do
                {
                    tokenizer.GoToNextToken();
                } while (tokenizer.currentToken != Token.ELSE &&
                    tokenizer.currentToken != Token.CR &&
                    tokenizer.currentToken != Token.ENDOFINPUT
                    );
                if (tokenizer.currentToken == Token.ELSE)
                {
                    tokenizer.GoToNextToken();
                    Statement();
                }
                else if (tokenizer.currentToken == Token.CR)
                {
                    tokenizer.GoToNextToken();
                }
            }
        }

        public static void GotoStatement()
        {
            Accept(Token.GOTO);
            int lineNumber = tokenizer.GetNumber();
            JumpToLine(lineNumber);
        }

        public static void GosubStatement()
        {
            int lineIndex = 0;
            Accept(Token.GOSUB);
            lineIndex = tokenizer.GetNumber();
            Accept(Token.NUMBER);
            Accept(Token.CR);
            if (gosubIndex < Max_Gosub_Stack_Depth)
            {
                gosubStack[gosubIndex] = tokenizer.LineNumber; //LineNumber + 1 ???
                gosubIndex++;
                JumpToLine(lineIndex);
            }
            else
            {
                throw new Exception("gosub stack overflow");
            }
        }

        public static void ReturnStatement()
        {
            Accept(Token.RETURN);
            if (gosubIndex > 0)
            {
                gosubIndex--;
                JumpToLine(gosubStack[gosubIndex]);
            }
            else
            {
                throw new Exception("Return statement: non-matching return");
            }
        }

        public static void ForStatement()
        {
            int forVariable = 0, to = 0;
            Accept(Token.FOR);
            forVariable = tokenizer.VariableNumber;
            Accept(Token.VARIABLE);
            Accept(Token.EQ);
            SetVariable(forVariable, Expr());
            Accept(Token.TO);
            to = Expr();
            Accept(Token.CR);
            if (forStackIndex < Max_For_Stack_Depth)
            {
                forStack[forStackIndex] = new ForState();
                forStack[forStackIndex].LineAfterFor = tokenizer.LineNumber;
                forStack[forStackIndex].ForVariable = forVariable;
                forStack[forStackIndex].To = to;
                forStackIndex++;
            }
            else
            {
                throw new Exception("for stack is overflown");
            }
        }

        public static void NextStatement()
        {
            int var = 0;
            Accept(Token.NEXT);
            var = tokenizer.VariableNumber;
            Accept(Token.VARIABLE);
            if (forStackIndex > 0 && var == forStack[forStackIndex - 1].ForVariable)
            {
                SetVariable(var, GetVariable(var) + 1);
                if (GetVariable(var) <= forStack[forStackIndex - 1].To)
                {
                    JumpToLine(forStack[forStackIndex - 1].LineAfterFor);
                }
                else
                {
                    forStackIndex--;
                    Accept(Token.CR);
                }
            }
            else
            {
                throw new Exception("Next Statement: Could not Find matching 'Next'");
            }
        }

        public static void EndStatement()
        {
            Accept(Token.END);
            Ended = true;
        }

        public static void LetStatement()
        {
            int var = tokenizer.VariableNumber;
            Accept(Token.VARIABLE);
            Accept(Token.EQ);
            SetVariable(var, Expr());
            //let statement: assigned to var
            Accept(Token.CR);
        }

        public static void Accept(Token token)
        {
            if (token != tokenizer.currentToken)
            {
                //token was not expected
                throw new Exception("got unacceptible token");
            }
            //token was expected
            
        }
        public static int Factor()
        {
            int res = 0;
            // factor token == tokenizer.currentToken
            switch (tokenizer.currentToken)
            {
                case Token.NUMBER:
                    res = tokenizer.GetNumber();
                    //factor number res
                    Accept(Token.NUMBER);
                    break;
                case Token.LEFTPAREN:
                    Accept(Token.LEFTPAREN);
                    res = Expr();
                    Accept(Token.RIGHTPAREN);
                    break;
                default:
                    res = VarFactor();
                    break;
            }
            return res;
        }
        public static int VarFactor()
        {
            int res = 0;
            //varfactor: obtaining variable tokenizer.VariableNumber
            res = GetVariable(tokenizer.VariableNumber);
            Accept(Token.VARIABLE);
            return res;
        }
        public static int Expr()
        {
            int t1 = 0, t2 = 0;
            Token op;
            t1 = Term();
            op = tokenizer.currentToken;
            //expr token == op
            while (op == Token.PLUS || op == Token.MINUS || op == Token.AND || op == Token.OR)
            {
                tokenizer.GoToNextToken();
                t2 = Term();
                //expr t1 op t2
                switch (op)
                {
                    case Token.PLUS:
                        t1 = t1 + t2;
                        break;
                    case Token.MINUS:
                        t1 = t1 - t2;
                        break;
                    case Token.AND:
                        t1 = t1 & t2;
                        break;
                    case Token.OR:
                        t1 = t1 | t2;
                        break;
                }
                op = tokenizer.currentToken;
            }
            return t1;
        }
        public static int Term()
        {
            int f1 = 0, f2 = 0;
            f1 = Factor();
            Token op = tokenizer.currentToken;
            //term token operation == op
            while (op == Token.ASTR || op == Token.SLASH || op == Token.MOD)
            {
                tokenizer.GoToNextToken();
                f2 = Factor();
                //term f1 op f2
                switch (op)
                {
                    case Token.ASTR:
                        f1 = f1 * f2;
                        break;
                    case Token.SLASH:
                        f1 = f1 / f2;
                        break;
                    case Token.MOD:
                        f1 = f1 % f2;
                        break;
                }
                op = tokenizer.currentToken;
            }
            return f1;
        }

        public static bool Relation()
        {
            int r1 = 0, r2 = 0;
            bool res = false;
            Token op;
            r1 = Expr();
            op = tokenizer.currentToken;
            //relation: token op
            while (op == Token.LT || op == Token.GT || op == Token.EQ)
            {
                tokenizer.GoToNextToken();
                r2 = Expr();
                //relation: r1 op r2
                switch (op)
                {
                    case Token.LT:
                        res = r1 < r2;
                        break;
                    case Token.GT:
                        res = r1 > r2;
                        break;
                    case Token.EQ:
                        res = r1 == r2;
                        break;
                }
            }
            return res;
        }

        public static void JumpToLine(int lineIndex)
        {
            if (lineIndex < 0)
	        {
		         throw new Exception("negative line index? seriously?")
	        }
            tokenizer = new Tokenizer(progText);

            while (tokenizer.LineNumber != lineIndex)
            {
                tokenizer.GoToNextToken();
                if (tokenizer.currentToken == Token.ENDOFINPUT)
                {
                    throw new Exception("I do not have so many lines of code");
                }
            }
            //found line lineIndex
        }

        public static void SetVariable(int varIndex, int value)
        {
            if (varIndex > 0 && varIndex < Max_Var_Count)
            {
                Variables[varIndex] = value;
            }
        }
        public static int GetVariable(int varIndex)
        {
            if (varIndex > 0 && varIndex < Max_Var_Count)
            {
                return Variables[varIndex];
            }
            return 0;
        }
    }
    public struct ForState
    {
        public int LineAfterFor;
        public int ForVariable;
        public int To;
        public ForState(int lineAfterFor, int forVariable, int to)
        {
            LineAfterFor = lineAfterFor;
            ForVariable = forVariable;
            To = to;
        }
        public ForState()
        {
            LineAfterFor = 0;
            ForVariable = 0;
            To = 0;
        }
    }
}
