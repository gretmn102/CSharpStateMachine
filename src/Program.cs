namespace App
{
    class Transition<Action, Context, Command, NodeKey>
        where Action : notnull
        where NodeKey : notnull
    {
        public Action Key { get; set; }

        public NodeKey? To { get; set; }

        public Func<Context, IEnumerator<Command>?>? Act { get; set; }

        public Transition(Action key)
        {
            Key = key;
        }
    }

    class Node<Action, Context, Command, NodeKey>
        where Action : notnull
        where NodeKey : notnull
    {
        public Dictionary<Action, Transition<Action, Context, Command, NodeKey>> Transitions { get; set; }

        public NodeKey Key { get; set; }

        public Node(
            NodeKey key,
            Dictionary<Action, Transition<Action, Context, Command, NodeKey>> transitions)
        {
            Key = key;
            Transitions = transitions;
        }

        public Node(
            NodeKey name,
            IEnumerable<Transition<Action, Context, Command, NodeKey>> transitions
        ) : this(name, new Dictionary<Action, Transition<Action, Context, Command, NodeKey>>())
        {
            Transitions = new Dictionary<Action, Transition<Action, Context, Command, NodeKey>>(
                transitions.Select(x =>
                    new KeyValuePair<Action, Transition<Action, Context, Command, NodeKey>>(x.Key, x))
            );
        }
    }

    class StateMachine<Action, Context, Command, NodeKey>
        where Action : notnull
        where NodeKey : notnull
    {
        private Dictionary<NodeKey, Node<Action, Context, Command, NodeKey>> _nodes;

        public Dictionary<NodeKey, Node<Action, Context, Command, NodeKey>> Nodes
        {
            get { return _nodes; }
            set
            {
                _nodes = value;
                SetCurrentNodeKey(CurrentNodeKey);
            }
        }

        Node<Action, Context, Command, NodeKey>? _currentNode;

        private NodeKey? _currentNodeKey;

        public void SetCurrentNodeKey(NodeKey? value)
        {
            _currentNodeKey = value;
            if (value != null)
            {
                _nodes.TryGetValue(value, out var result);
                _currentNode = result;
            }
        }

        public NodeKey? CurrentNodeKey
        {
            get { return _currentNodeKey; }
            set { SetCurrentNodeKey(value); }
        }

        public Context State { get; set; }

        public Action<IEnumerator<Command>> Interpret { get; set; }

        public StateMachine(
            Dictionary<NodeKey, Node<Action, Context, Command, NodeKey>> nodes,
            NodeKey initNode,
            Action<IEnumerator<Command>> interpret,
            Context context)
        {
            _nodes = nodes; // to avoid csharp(CS8618)
            Nodes = nodes;
            CurrentNodeKey = initNode;
            Interpret = interpret;
            State = context;
        }

        public StateMachine(
            IEnumerable<Node<Action, Context, Command, NodeKey>> nodes,
            NodeKey initNode,
            Action<IEnumerator<Command>> interpret,
            Context context
        ) : this(new Dictionary<NodeKey, Node<Action, Context, Command, NodeKey>>(), initNode, interpret, context)
        {
            Nodes = new Dictionary<NodeKey, Node<Action, Context, Command, NodeKey>>(
                nodes.Select(x =>
                    new KeyValuePair<NodeKey, Node<Action, Context, Command, NodeKey>>(x.Key, x))
            );
        }

        public void Do(Action action)
        {
            if (_currentNode == null)
            {
                throw new Exception($"_currentNode is null {action}");
            }
            else
            {
                if (!_currentNode.Transitions.TryGetValue(action, out var transition))
                {
                    throw new Exception($"Not found {action}");
                }
                else
                {
                    CurrentNodeKey = transition.To;
                    var act = transition.Act;
                    if (act == null)
                    {
                        return;
                    }
                    else
                    {
                        var res = act.Invoke(State);
                        if (res == null)
                        {
                            return;
                        }
                        else
                        {
                            Interpret(res);
                        }
                    }
                }
            }
        }
    }

    enum NodeKey
    {
        InputInitCounter,
        Active,
        Inactive,
    }

    enum Action
    {
        Toggle,
        Input
    }

    class Counter
    {
        public int Accumulator { get; set; }

        public void Increment()
        {
            Accumulator++;
        }

        public void Decrement()
        {
            Accumulator--;
        }

        public Counter(int accumulator)
        {
            Accumulator = accumulator;
        }

        public override string ToString()
        {
            return Accumulator.ToString();
        }
    }

    interface ICommand { }

    class PrintCommand : ICommand
    {
        public string Text { get; set; }

        public PrintCommand(string text)
        {
            Text = text;
        }
    }

    class ReadIntCommand : ICommand
    {
        public Func<int>? ReadInt { get; set; }
    }

    class TogglerWithCounter
    {
        public StateMachine<Action, Counter, ICommand, NodeKey> StateMachine { get; set; }

        private static IEnumerator<ICommand> InputInitCounterHandler(Counter counter)
        {
            yield return new PrintCommand("Input init counter handler");

            ReadIntCommand rlc = new();
            yield return rlc;
            counter.Accumulator = rlc.ReadInt switch
            {
                null => throw new Exception("ReadLine is null"),
                _ => rlc.ReadInt(),
            };
        }

        private static Node<Action, Counter, ICommand, NodeKey> CreateInputInitCounter()
        {
            Transition<Action, Counter, ICommand, NodeKey> inputInitCounter = new(Action.Input)
            {
                Act = InputInitCounterHandler,
                To = NodeKey.Inactive,
            };

            Node<Action, Counter, ICommand, NodeKey> inputInitCounterNode = new(
                NodeKey.InputInitCounter,
                new Transition<Action, Counter, ICommand, NodeKey>[] { inputInitCounter }
            );

            return inputInitCounterNode;
        }

        private static IEnumerator<ICommand> InactiveHandler(Counter counter)
        {
            counter.Increment();
            yield return new PrintCommand($"Inactive -> Active {counter} times");
        }

        private static Node<Action, Counter, ICommand, NodeKey> CreateInactive()
        {
            Transition<Action, Counter, ICommand, NodeKey> inactive = new(Action.Toggle)
            {
                Act = InactiveHandler,
                To = NodeKey.Active
            };

            Node<Action, Counter, ICommand, NodeKey> inactiveNode = new(
                NodeKey.Inactive,
                new Transition<Action, Counter, ICommand, NodeKey>[] { inactive }
            );
            return inactiveNode;
        }

        private static IEnumerator<ICommand> ActiveHandler(Counter counter)
        {
            counter.Increment();
            yield return new PrintCommand($"Active -> Inactive {counter} times");
        }

        private static Node<Action, Counter, ICommand, NodeKey> CreateActive()
        {
            Transition<Action, Counter, ICommand, NodeKey> active = new(Action.Toggle)
            {
                Act = ActiveHandler,
                To = NodeKey.Inactive
            };

            Node<Action, Counter, ICommand, NodeKey> activeNode = new(
                NodeKey.Active,
                new Transition<Action, Counter, ICommand, NodeKey>[] { active }
            );
            return activeNode;
        }

        static void Interpret(IEnumerator<ICommand> commands)
        {
            while (commands.MoveNext())
            {
                switch (commands.Current)
                {
                    case PrintCommand printCommand:
                        Console.WriteLine(printCommand.Text);
                        break;

                    case ReadIntCommand readIntCommand:
                        readIntCommand.ReadInt = () =>
                        {
                            string? input;
                            while (true)
                            {
                                input = Console.ReadLine();
                                if (int.TryParse(input, out int result))
                                {
                                    return result;
                                }
                            }
                        };
                        break;

                    default:
                        throw new Exception($"{commands.Current.GetType().FullName} not implemented yet");
                }
            }
        }

        public TogglerWithCounter()
        {
            var init = CreateInputInitCounter();

            var nodes = new Node<Action, Counter, ICommand, NodeKey>[] {
                CreateInputInitCounter(),
                CreateActive(),
                CreateInactive(),
            };

            Counter counter = new(0);

            StateMachine = new(nodes, init.Key, Interpret, counter);
        }
    }

    internal class Program
    {
        public static void Main()
        {
            TogglerWithCounter togglerWithCounter = new();
            var toggler = togglerWithCounter.StateMachine;
            toggler.Do(Action.Input);
            toggler.Do(Action.Toggle);
            toggler.Do(Action.Toggle);
            toggler.Do(Action.Toggle);
        }
    }
}
