using Microsoft.AspNetCore.Mvc;

namespace Adriva.Extensions.Reports.Mvc
{
    public class MvcRendererContext
    {
        public ActionContext ActionContext { get; private set; }

        public string ViewName { get; set; }

        public MvcRendererContext(ActionContext actionContext, string viewName = null)
        {
            this.ActionContext = actionContext;
            this.ViewName = viewName;
        }
    }
}