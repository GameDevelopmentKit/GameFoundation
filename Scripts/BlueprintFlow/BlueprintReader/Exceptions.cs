namespace BlueprintFlow.BlueprintReader
{
    using System;

    public class FieldDontExistInBlueprint : Exception
    {
        public FieldDontExistInBlueprint(string message) : base(message)
        {
        }
    }
}