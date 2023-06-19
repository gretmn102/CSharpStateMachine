namespace App
{
    class Transition<Action, Context, Command> where Action : notnull
    {
        public Action Key { get; set; }

        public Node<Action, Context, Command>? To { get; set; }

        public Func<Context, IEnumerator<Command>?>? Act { get; set; }

        public Transition(Action key)
        {
            Key = key;
        }
    }

    class Node<Action, Context, Command> where Action : notnull
    {
        public Dictionary<Action, Transition<Action, Context, Command>> Transitions { get; set; }

        public Node(Dictionary<Action, Transition<Action, Context, Command>> transitions)
        {
            Transitions = transitions;
        }

        public Node(IEnumerable<Transition<Action, Context, Command>> transitions)
        {
            Transitions = new Dictionary<Action, Transition<Action, Context, Command>>(
                transitions.Select(x =>
                    new KeyValuePair<Action, Transition<Action, Context, Command>>(x.Key, x))
            );
        }
    }

    class StateMachine<Action, Context, Command> where Action : notnull
    {
        Node<Action, Context, Command>? _currentNode;

        public Context State { get; set; }

        public Action<IEnumerator<Command>> Interpret { get; set; }

        public StateMachine(Node<Action, Context, Command> initNode, Action<IEnumerator<Command>> interpret, Context context)
        {
            _currentNode = initNode;
            Interpret = interpret;
            State = context;
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
                    _currentNode = transition.To;
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

    enum Action
    {
        Toggle = 0
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

    struct PrintCommand : ICommand
    {
        public string Text { get; set; }

        public PrintCommand(string text)
        {
            Text = text;
        }
    }

    struct ReadLineCommand : ICommand
    {
        public Func<string> Result { get; set; }

    }

    class TogglerWithCounter
    {
        public StateMachine<Action, Counter, ICommand> StateMachine { get; set; }

        private static IEnumerator<ICommand> InactiveHandler(Counter counter)
        {
            counter.Increment();
            yield return new PrintCommand($"Inactive -> Active {counter} times");
        }

        private static IEnumerator<ICommand> ActiveHandler(Counter counter)
        {
            counter.Increment();
            yield return new PrintCommand($"Active -> Inactive {counter} times");
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
                }
            }
        }

        public TogglerWithCounter()
        {
            Transition<Action, Counter, ICommand> active = new(Action.Toggle)
            {
                Act = ActiveHandler,
                To = null
            };

            Node<Action, Counter, ICommand> activeNode = new(
                new Transition<Action, Counter, ICommand>[] { active }
            );

            Transition<Action, Counter, ICommand> inactive = new(Action.Toggle)
            {
                Act = InactiveHandler,
                To = activeNode
            };

            Node<Action, Counter, ICommand> inactiveNode = new(
                new Transition<Action, Counter, ICommand>[] { inactive }
            );

            active.To = inactiveNode;

            Counter counter = new(0);

            StateMachine = new(inactiveNode, Interpret, counter);
        }
    }

    internal class Program
    {
        public static void Main()
        {
            TogglerWithCounter togglerWithCounter = new();
            var toggler = togglerWithCounter.StateMachine;
            toggler.Do(Action.Toggle);
            toggler.Do(Action.Toggle);
            toggler.Do(Action.Toggle);
            toggler.Do(Action.Toggle);
        }
    }
}
