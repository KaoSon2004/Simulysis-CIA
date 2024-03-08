using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Types
{
    public class ColumnProp
    {
        private readonly string _propertyName;

        private readonly string _displayName;

        private readonly double _widthPercentage;
        public string PropertyName { get { return _propertyName; } }
        public string DisplayName { get { return _displayName; } }
        public double WidthPercentage { get { return (_widthPercentage / 10) * 9.5; } }

        public ColumnProp(string propertyName, string displayName, double widthPercentage)
        {
            _propertyName = propertyName;
            _displayName = displayName;
            _widthPercentage = widthPercentage;
        }
    }
}
