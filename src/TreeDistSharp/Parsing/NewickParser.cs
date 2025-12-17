using TreeDistSharp.Core;

namespace TreeDistSharp.Parsing;

/// <summary>
/// Parser for Newick format phylogenetic trees.
/// </summary>
public static class NewickParser
{
    /// <summary>
    /// Parse a Newick format string into a Tree.
    /// </summary>
    /// <param name="newick">Newick string (with or without trailing semicolon)</param>
    /// <returns>Parsed tree</returns>
    /// <exception cref="FormatException">If the Newick string is malformed</exception>
    public static Tree Parse(string newick)
    {
        if (string.IsNullOrWhiteSpace(newick))
        {
            throw new FormatException("Empty Newick string");
        }

        var parser = new Parser(newick);
        var root = parser.ParseTree();
        return new Tree(root);
    }

    /// <summary>
    /// Parse a Newick format string into a Tree, using the specified tip label order.
    /// Useful when comparing trees that should have the same tip ordering.
    /// </summary>
    public static Tree Parse(string newick, IReadOnlyList<string> tipLabels)
    {
        if (string.IsNullOrWhiteSpace(newick))
        {
            throw new FormatException("Empty Newick string");
        }

        var parser = new Parser(newick);
        var root = parser.ParseTree();
        return new Tree(root, tipLabels);
    }

    private ref struct Parser
    {
        private readonly ReadOnlySpan<char> _input;
        private int _position;

        public Parser(string input)
        {
            _input = input.AsSpan();
            _position = 0;
        }

        public Node ParseTree()
        {
            SkipWhitespace();
            var root = ParseSubtree();
            SkipWhitespace();

            // Optional trailing semicolon
            if (_position < _input.Length && _input[_position] == ';')
            {
                _position++;
            }

            return root;
        }

        private Node ParseSubtree()
        {
            SkipWhitespace();

            if (_position >= _input.Length)
            {
                throw new FormatException("Unexpected end of input");
            }

            if (_input[_position] == '(')
            {
                // Internal node
                return ParseInternal();
            }
            else
            {
                // Leaf node
                return ParseLeaf();
            }
        }

        private Node ParseInternal()
        {
            // Consume '('
            _position++;
            SkipWhitespace();

            var children = new List<Node>();

            // Parse first child
            children.Add(ParseSubtree());
            SkipWhitespace();

            // Parse remaining children
            while (_position < _input.Length && _input[_position] == ',')
            {
                _position++; // consume ','
                SkipWhitespace();
                children.Add(ParseSubtree());
                SkipWhitespace();
            }

            // Consume ')'
            if (_position >= _input.Length || _input[_position] != ')')
            {
                throw new FormatException($"Expected ')' at position {_position}");
            }
            _position++;

            // Parse optional label and branch length
            var label = ParseLabel();
            ParseBranchLength(); // Ignore branch length

            return new Node(children, string.IsNullOrEmpty(label) ? null : label);
        }

        private Node ParseLeaf()
        {
            var label = ParseLabel();

            if (string.IsNullOrEmpty(label))
            {
                throw new FormatException($"Expected leaf label at position {_position}");
            }

            ParseBranchLength(); // Ignore branch length

            return new Node(label);
        }

        private string ParseLabel()
        {
            SkipWhitespace();

            int start = _position;

            // Check for quoted label
            if (_position < _input.Length && _input[_position] == '\'')
            {
                _position++; // consume opening quote
                start = _position;
                while (_position < _input.Length && _input[_position] != '\'')
                {
                    _position++;
                }
                int end = _position;
                if (_position < _input.Length)
                {
                    _position++; // consume closing quote
                }
                return _input[start..end].ToString();
            }

            // Unquoted label - read until special character
            while (_position < _input.Length)
            {
                char c = _input[_position];
                if (c == '(' || c == ')' || c == ',' || c == ':' || c == ';' || char.IsWhiteSpace(c))
                {
                    break;
                }
                _position++;
            }

            return _input[start.._position].ToString();
        }

        private void ParseBranchLength()
        {
            SkipWhitespace();

            if (_position < _input.Length && _input[_position] == ':')
            {
                _position++; // consume ':'
                SkipWhitespace();

                // Parse the number (ignore the value)
                while (_position < _input.Length)
                {
                    char c = _input[_position];
                    if (char.IsDigit(c) || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                    {
                        _position++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void SkipWhitespace()
        {
            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }
        }
    }
}
