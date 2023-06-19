namespace App
{
    using Node = StateMachine.Node<NodeKey, Action, Counter, ICommand>;
    using Transition = StateMachine.Transition<NodeKey, Action, Counter, ICommand>;
    using Machine = StateMachine.Machine<NodeKey, Action, Counter, ICommand>;

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
        public Machine StateMachine { get; set; }

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

        private static Node CreateInputInitCounter()
        {
            Transition inputInitCounter = new(Action.Input)
            {
                Act = InputInitCounterHandler,
                To = NodeKey.Inactive,
            };

            Node inputInitCounterNode = new(
                NodeKey.InputInitCounter,
                new Transition[] { inputInitCounter }
            );

            return inputInitCounterNode;
        }

        private static IEnumerator<ICommand> InactiveHandler(Counter counter)
        {
            counter.Increment();
            yield return new PrintCommand($"Inactive -> Active {counter} times");
        }

        private static Node CreateInactive()
        {
            Transition inactive = new(Action.Toggle)
            {
                Act = InactiveHandler,
                To = NodeKey.Active
            };

            Node inactiveNode = new(
                NodeKey.Inactive,
                new Transition[] { inactive }
            );
            return inactiveNode;
        }

        private static IEnumerator<ICommand> ActiveHandler(Counter counter)
        {
            counter.Increment();
            yield return new PrintCommand($"Active -> Inactive {counter} times");
        }

        private static Node CreateActive()
        {
            Transition active = new(Action.Toggle)
            {
                Act = ActiveHandler,
                To = NodeKey.Inactive
            };

            Node activeNode = new(
                NodeKey.Active,
                new Transition[] { active }
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

            var nodes = new Node[] {
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
