using Qconcert.Models;

namespace Qconcert.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<VipPromotionViewModel> VipEvents { get; set; }
        public List<RegularPromotionViewModel> RegularEvents { get; set; }
        public List<IGrouping<Category, Event>> CategorizedEvents { get; set; }
    }

    public class VipPromotionViewModel
    {
        public Event Event { get; set; }
        public string MediaPath { get; set; }
    }

    public class RegularPromotionViewModel
    {
        public Event Event { get; set; }
        public string MediaPath { get; set; } // Dùng chung như VIP
    }
}
