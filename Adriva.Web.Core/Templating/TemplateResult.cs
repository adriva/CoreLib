namespace Adriva.Web.Core.Templating
{
    public class TemplateResult
    {
        public bool IsSuccess { get; private set; }

        public string Text { get; private set; }

        public string ErrorMessage { get; private set; }

        private TemplateResult()
        {

        }

        public static TemplateResult CreateError(string errorMessage)
        {
            return new TemplateResult()
            {
                IsSuccess = false,
                Text = null,
                ErrorMessage = errorMessage
            };
        }

        public static TemplateResult CreateSuccess(string text)
        {
            return new TemplateResult()
            {
                IsSuccess = true,
                ErrorMessage = null,
                Text = text
            };
        }

        public override string ToString()
        {
            return !this.IsSuccess ? $"Error: '{this.ErrorMessage}'" : this.Text;
        }
    }
}
