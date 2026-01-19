using System;
using System.Windows.Markup;

namespace PlayCutWin.Helpers
{
    // XAMLで <helpers:BooleanBox>True</helpers:BooleanBox> みたいに使える
    [ContentProperty(nameof(Value))]
    public sealed class BooleanBox : MarkupExtension
    {
        public bool Value { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
            => Value ? TrueBox : FalseBox;

        private static readonly object TrueBox = true;
        private static readonly object FalseBox = false;
    }
}
