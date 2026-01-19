using System;
using System.Windows.Markup;

namespace PlayCutWin.Helpers
{
    /// <summary>
    /// XAMLから bool を object として渡すための箱（CommandParameter用）
    /// 例: CommandParameter="{helpers:BooleanBox True}"
    /// </summary>
    [MarkupExtensionReturnType(typeof(bool))]
    public class BooleanBox : MarkupExtension
    {
        public bool Value { get; set; }

        public BooleanBox() { }

        public BooleanBox(bool value)
        {
            Value = value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Value;
        }
    }
}
