using System;

namespace DatingApp.API.DTOs
{
    public class PhotosForDetailedDto
    {
        public int PhotoID { get; set; }

        public string URL { get; set; }

        public string Description { get; set; }
 
        public DateTime DateAdded { get; set; }

        public bool IsMain { get; set; }
    }
}