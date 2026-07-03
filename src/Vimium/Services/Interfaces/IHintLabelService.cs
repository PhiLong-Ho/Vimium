using System.Collections.Generic;

namespace Vimium.Services.Interfaces
{
    public interface IHintLabelService
    {
        /// <summary>
        /// Generate N labels
        /// </summary>
        IList<string> GetHintStrings(int count);
    }
}
