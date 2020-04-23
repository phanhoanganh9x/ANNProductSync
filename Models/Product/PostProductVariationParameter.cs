using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class PostProductVariationParameter
    {
        [Required(ErrorMessage = "wcProductID là biến bắt buộc")]
        public int wcProductID { get; set; }
    }
}
