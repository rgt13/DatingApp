using System;

namespace DatingApp.API.Models
{
    public class Photo
    {
        public int PhotoID { get; set; }

        public string URL { get; set; }

        public string Description { get; set; }
 
        public DateTime DateAdded { get; set; }

        public bool IsMain { get; set; }

        public User User { get; set; }

        public int UserID { get; set; }
    }
}