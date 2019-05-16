using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerlessDemo.OrderFood
{
    /// <summary>
    /// A utility class to store all the current values from the intent's slots.
    /// </summary>
    public class FoodOrder
    {
        public FoodTypes? FoodType { get; set; }

        public string PickUpTime { get; set; }

        public string PickUpDate { get; set; }



        [JsonIgnore]
        public bool HasRequiredFoodFields
        {
            get
            {
                return !string.IsNullOrEmpty(PickUpDate)
                        && !string.IsNullOrEmpty(PickUpTime)
                        && !string.IsNullOrEmpty(FoodType.ToString());

            }
        }



        public enum FoodTypes
        {
            Pizza,
            Wings,
            Beer,
            Null
        }
    }
}
