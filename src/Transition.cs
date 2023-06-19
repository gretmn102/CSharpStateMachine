namespace StateMachine
{
    class Transition<NodeKey, Action, Context, Command>
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
}
