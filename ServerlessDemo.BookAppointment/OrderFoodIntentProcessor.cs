using Amazon.Lambda.Core;
using Amazon.Lambda.LexEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using static ServerlessDemo.OrderFood.FoodOrder;

namespace ServerlessDemo.OrderFood
{
    public class OrderFoodIntentProcessor : AbstractIntentProcessor
    {
        public const string TYPE_SLOT = "FoodType";
        public const string PICK_UP_DATE_SLOT = "PickupDate";
        public const string PICK_UP_TIME_SLOT = "PickupTime";
        public const string INVOCATION_SOURCE = "invocationSource";
        FoodTypes _chosenFoodType = FoodTypes.Null;

        /// <summary>
        /// Performs dialog management and fulfillment for ordering Foods.
        /// 
        /// Beyond fulfillment, the implementation for this intent demonstrates the following:
        /// 1) Use of elicitSlot in slot validation and re-prompting
        /// 2) Use of sessionAttributes to pass information that can be used to guide the conversation
        /// </summary>
        /// <param name="lexEvent"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override LexResponse Process(LexEvent lexEvent, ILambdaContext context)
        {
            IDictionary<string, string> slots = lexEvent.CurrentIntent.Slots;
            IDictionary<string, string> sessionAttributes = lexEvent.SessionAttributes ?? new Dictionary<string, string>();

            //if all the values in the slots are empty return the delegate, theres nothing to validate or do.
            if (slots.All(x => x.Value == null))
            {
                return Delegate(sessionAttributes, slots);
            }
			
            //if the Food slot has a value, validate that it is contained within the enum list available.
            if (slots[TYPE_SLOT] != null)
            {
                var validateFoodType = ValidateFoodType(slots[TYPE_SLOT]);

                if (!validateFoodType.IsValid)
                {
                    slots[validateFoodType.ViolationSlot] = null;
                    return ElicitSlot(sessionAttributes, lexEvent.CurrentIntent.Name, slots, validateFoodType.ViolationSlot, validateFoodType.Message);
                }
            }

            //now that enum has been parsed and validated, create the order
            FoodOrder order = CreateOrder(slots);

            if (string.Equals(lexEvent.InvocationSource, "DialogCodeHook", StringComparison.Ordinal))
            {
                //validate the remaining slots.
                var validateResult = Validate(order);
                // If any slots are invalid, re-elicit for their value


                if (!validateResult.IsValid)
                {
                    slots[validateResult.ViolationSlot] = null;
                    return ElicitSlot(sessionAttributes, lexEvent.CurrentIntent.Name, slots, validateResult.ViolationSlot, validateResult.Message);
                }


                // Pass the price of the Foods back through session attributes to be used in various prompts defined
                // on the bot model.
                if (order.FoodType.Value != FoodTypes.Null)
                {
                    sessionAttributes["Price"] = (order.FoodType.Value.ToString().Length * 5).ToString();
                }


                return Delegate(sessionAttributes, slots);
            }


            return Close(
                        sessionAttributes,
                        "Fulfilled",
                        new LexResponse.LexMessage
                        {
                            ContentType = MESSAGE_CONTENT_TYPE,
                            Content = String.Format("Thanks, your order for {0} has been placed and will be ready for pickup by {1} on {2}.", order.FoodType.ToString(), order.PickUpTime, order.PickUpDate)
                        }
                    );
        }

        private FoodOrder CreateOrder(IDictionary<string, string> slots)
        {


            FoodOrder order = new FoodOrder
            {
                FoodType = _chosenFoodType,
                PickUpDate = slots.ContainsKey(PICK_UP_DATE_SLOT) ? slots[PICK_UP_DATE_SLOT] : null,
                PickUpTime = slots.ContainsKey(PICK_UP_TIME_SLOT) ? slots[PICK_UP_TIME_SLOT] : null
            };

            return order;
        }

        /// <summary>
        /// Verifies that any values for slots in the intent are valid.
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private ValidationResult Validate(FoodOrder order)
        {

            if (!string.IsNullOrEmpty(order.PickUpDate))
            {
                DateTime pickUpDate = DateTime.MinValue;
                if (!DateTime.TryParse(order.PickUpDate, out pickUpDate))
                {
                    return new ValidationResult(false, PICK_UP_DATE_SLOT,
                        "I did not understand that, what date would you like to pick food up?");
                }
                if (pickUpDate <= DateTime.Today)
                {
                    return new ValidationResult(false, PICK_UP_DATE_SLOT,
                        "You can't pick food up in the past. Try again.");
                }
            }

            if (!string.IsNullOrEmpty(order.PickUpTime))
            {
                string[] timeComponents = order.PickUpTime.Split(":");
                Double hour = Double.Parse(timeComponents[0]);
                Double minutes = Double.Parse(timeComponents[1]);

                if (Double.IsNaN(hour) || Double.IsNaN(minutes))
                {
                    return new ValidationResult(false, PICK_UP_TIME_SLOT, null);
                }

                if (hour < 10 || hour >= 21)
                {
                    return new ValidationResult(false, PICK_UP_TIME_SLOT, "Our business hours are from ten a m. to 9 p m. Can you specify a time during this range?");
                }

            }

            return ValidationResult.VALID_RESULT;
        }

        /// <summary>
        /// Verifies that any values for Food type slot in the intent is valid.
        /// </summary>
        /// <param name="FoodtypeString"></param>
        /// <returns></returns>
        private ValidationResult ValidateFoodType(string FoodTypeString)
        {
            bool isFoodTypeValid = Enum.IsDefined(typeof(FoodTypes), FoodTypeString.ToUpper());

            if (Enum.TryParse(typeof(FoodTypes), FoodTypeString, true, out object FoodType))
            {
                _chosenFoodType = (FoodTypes)FoodType;
                return ValidationResult.VALID_RESULT;
            }
            else
            {
                return new ValidationResult(false, TYPE_SLOT, String.Format("We do not have {0}, would you like a different type of food? Our most popular food is pizza, duh.", FoodTypeString));
            }
        }

    }

}
