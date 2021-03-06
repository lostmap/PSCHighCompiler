﻿using System;

namespace parser
{
    using lexer;
    using symbols;
    using inter;

    public class Parser
    {
        private Lexer lex;    // lexical analyzer for this parser
        private Token look;   // lookahead tagen
        internal Env top = null;       // current or top symbol table
        internal  int used = 0;         // storage used for declarations

        public Parser(Lexer l)
        {
            lex = l;
            move();
        }

        internal virtual void move() 
        {
            look = lex.scan();
        }

        internal virtual void error(string s)
        {
            throw new Exception("near line " + Lexer.line + ": " + s);
        }

        internal virtual void match(int t)
        {
            if (look.tag == t)
            {
                move();
            }
            else
            {
                error("syntax error");
            }
        }

        public virtual void program()
        {   // program -> block
            Stmt s = block();
            int begin = s.newlabel();
            int after = s.newlabel();
            s.emitlabel(begin);
            s.gen(begin, after);
            s.bytecode(top);
            s.emitlabel(after);
        }

        internal virtual Stmt block()
        {   // block -> { decls stmts }
            match('{'); Env savedEnv = top; top = new Env(top);
            decls(); Stmt s = stmts();
            match('}'); top = savedEnv;
            return s;
       }

        internal virtual void decls()
        {
            while (look.tag == Tag.BASIC)
            {   // D -> type ID ;
                Type p = type(); 
                Token tok = look;
                match(Tag.ID);
                match(';');
                Id id = new Id((Word)tok, p, used);
                top.put(tok, id);
                used = used + p.width; // change later
            }
        }

        internal virtual Type type()
        {
            Type p = (Type)look;            // expect look.tag == Tag.BASIC 
            match(Tag.BASIC);
            if (look.tag != '[')
            {
                return p;                   // T -> basic
            }
            else
            {
                return dims(p);             // return array type
            }
        }

        internal virtual Type dims(Type p)
        {
            match('['); 
            Token tok = look;
            match(Tag.NUM);
            match(']');
            if (look.tag == '[')
            {
                p = dims(p);
            }
            return new Array(((Num) tok).value, p);
        }

        internal virtual Stmt stmts()
        {
            if (look.tag == '}')
            {
                return Stmt.Null;
            }
            else
            {
                return new Seq(stmt(), stmts());
            }
        }

        internal virtual Stmt stmt()
        {
            Expr x;
            Stmt s, s1, s2;
            Stmt savedStmt;         // save enclosing loop for breaks

            switch (look.tag)
            {
                case ';':
                    move();
                    return Stmt.Null;
                case Tag.IF:
                    match(Tag.IF);
                    match('(');
                    x = boolean();
                    match(')');
                    s1 = stmt();
                    if (look.tag != Tag.ELSE)
                    {
                        return new If(x, s1);
                    }
                    match(Tag.ELSE);
                    s2 = stmt();
                    return new Else(x, s1, s2);

                case Tag.WHILE:
                    While whilenode = new While();
                    savedStmt = Stmt.Enclosing;
                    Stmt.Enclosing = whilenode;
                    match(Tag.WHILE);
                    match('('); x = boolean();
                    match(')');
                    s1 = stmt();
                    whilenode.init(x, s1);
                    Stmt.Enclosing = savedStmt;  // reset Stmt.Enclosing
                    return whilenode;

                case Tag.DO:
                    Do donode = new Do();
                    savedStmt = Stmt.Enclosing;
                    Stmt.Enclosing = donode;
                    match(Tag.DO);
                    s1 = stmt();
                    match(Tag.WHILE);
                    match('('); 
                    x = boolean(); 
                    match(')');
                    match(';');
                    donode.init(s1, x);
                    Stmt.Enclosing = savedStmt;  // reset Stmt.Enclosing
                    return donode;

                case Tag.BREAK:
                    match(Tag.BREAK); match(';');
                    return new Break();

                case '{':
                    return block();

                default:
                    return assign();
            }
        }

        internal virtual Stmt assign()
        {
            Stmt stmt;
            Token t = look;
            match(Tag.ID);
            Id id = top.get(t);
            if (id == null)
            {
                error(t.ToString() + " undeclared");
            }
            if (look.tag == '=')
            {       // S -> id = E ;
                move();
                stmt = new Set(id, boolean());
            }
            else
            {                        // S -> L = E ;
                Access x = offset(id);
                match('=');
                stmt = new SetElem(x, boolean());
            }
            match(';');
            return stmt;
        }

        internal virtual Expr boolean()
        {
            Expr x = join();
            while (look.tag == Tag.OR)
            {
                Token tok = look;
                move();
                x = new Or(tok, x, join());
            }
            return x;
        }

        internal virtual Expr join()
        {
            Expr x = equality();
            while (look.tag == Tag.AND)
            {
                Token tok = look;
                move();
                x = new And(tok, x, equality());
            }
            return x;
        }

        internal virtual Expr equality()
        {
            Expr x = rel();
            while (look.tag == Tag.EQ || look.tag == Tag.NE)
            {
                Token tok = look;
                move();
                x = new Rel(tok, x, rel());
            }
            return x;
        }

        internal virtual Expr rel()
        {
            Expr x = expr();
            switch (look.tag)
            {
                case '<': case Tag.LE: case Tag.GE: case '>':
                    Token tok = look;
                    move();
                    return new Rel(tok, x, expr());
                default:
                    return x;
            }
        }

        internal virtual Expr expr()
        {
            Expr x = term();
            while (look.tag == '+' || look.tag == '-')
            {
                Token tok = look;
                move();
                x = new Arith(tok, x, term());
            }
            return x;
        }

        internal virtual Expr term()
        {
            Expr x = unary();
            while (look.tag == '*' || look.tag == '/')
            {
                Token tok = look;
                move();
                x = new Arith(tok, x, unary());
            }
            return x;
        }

        internal virtual Expr unary()
        {
            if (look.tag == '-')
            {
                move();
                return new Unary(Word.minus, unary());
            }
            else if (look.tag == '!')
            {
                Token tok = look;
                move();
                return new Not(tok, unary());
            }
            else
            {
                return factor();
            }
        }

        internal virtual Expr factor()
        {
            Expr x = null;
            switch (look.tag)
            {
                case '(':
                    move(); 
                    x = boolean();
                    match(')');
                    return x;
                case Tag.NUM:
                    x = new Constant(look, Type.Int);
                    move();
                    return x;
                case Tag.REAL:
                    x = new Constant(look, Type.Double);
                    move();
                    return x;
                case Tag.TRUE:
                    x = Constant.True;
                    move();
                    return x;
                case Tag.FALSE:
                    x = Constant.False; 
                    move();
                    return x;
                default:
                    error("syntax error");
                    return x;
                case Tag.ID:
                    string s = look.ToString();
                    Id id = top.get(look);
                    if (id == null)
                    {
                        error(look.ToString() + " undeclared");
                    }
                    move();
                    if (look.tag != '[')
                    {
                        return id;
                    }
                    else return offset(id);
            }
        }

        internal virtual Access offset(Id a)
        {   // I -> [E] | [E] I
            Expr i;
            Expr w;
            Expr t1, t2;
            Expr loc;  // inherit id

            Type type = a.type;
            match('[');
            i = boolean();
            match(']');     // first index, I -> [ E ]
            type = ((Array)type).of;
            w = new Constant(type.width);
            t1 = new Arith(new Token('*'), i, w);
            loc = t1;
            while (look.tag == '[')
            {      // multi-dimensional I -> [ E ] I
                match('['); i = boolean(); match(']');
                type = ((Array)type).of;
                w = new Constant(type.width);
                t1 = new Arith(new Token('*'), i, w);
                t2 = new Arith(new Token('+'), loc, t1);
                loc = t2;
            }

            return new Access(a, loc, type);
        }
    }
}
