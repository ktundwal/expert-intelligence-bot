using System;
using System.Collections.Generic;
using System.Text;

namespace UpworkAPI.Models
{
    public class Category
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public List<Category> Topics { get; set; }
    }
}
