
library(dplyr)
library(ggplot2)
library(forecast)
library(car)
library(lubridate)

food_delivery_data <- read.csv("food_delivery_data.csv")

head(food_delivery_data)

food_delivery_data <- food_delivery_data %>% distinct()

food_delivery_data <- food_delivery_data %>%
  mutate(across(everything(), ~ ifelse(is.na(.), mean(., na.rm = TRUE), .)))

food_delivery_data$Order_Date <- as.Date(food_delivery_data$Order_Date, format="%d-%m-%Y")

food_delivery_data$Time_Orderd <- hms::as_hms(food_delivery_data$Time_Orderd)

food_delivery_data$order_hour <- hour(food_delivery_data$Time_Orderd)

ggplot(food_delivery_data, aes(x = Time_taken.min.)) +
  geom_histogram(binwidth = 5, fill = "darkgreen", color = "black") +
  labs(title = "Distribution of Delivery Time", x = "Delivery Time (min)",y = "Frequency")


ggplot(food_delivery_data, aes(x = Delivery_person_Ratings)) +
  geom_bar(fill = "red", color = "black") +
  labs(title = "Delivery Person Ratings", x = "Ratings", y = "Count")


traffic_density_counts <- food_delivery_data %>%
  count(Road_traffic_density) %>%
  mutate(percentage = n / sum(n) * 100)

ggplot(traffic_density_counts, aes(x = "", y = percentage, 
                                   fill = Road_traffic_density)) +
  geom_bar(stat = "identity", width = 1) +
  coord_polar("y") +
  labs(title = "Orders by Road Traffic Density", x = "", y = "") +
  theme_void()


ggplot(food_delivery_data, aes(x = factor(multiple_deliveries))) +
  geom_bar(fill = "darkgreen", color = "black") +
  labs(title = "Distribution of Multiple Deliveries", x = "Multiple Deliveries", 
       y = "Count")


rating_by_city <- food_delivery_data %>%
  group_by(City) %>%
  summarise(avg_rating = mean(Delivery_person_Ratings, na.rm = TRUE))

ggplot(rating_by_city, aes(x = City, y = avg_rating, fill = avg_rating)) +
  geom_tile() +
  scale_fill_gradient(low = "darkgreen", high = "lightgreen") +
  labs(title = "Average Delivery Person Ratings by City", x = "City", 
       y = "Average Rating")



ggplot(food_delivery_data, aes(x = Weatherconditions, y = Delivery_person_Ratings)) +
  geom_bar(stat = "identity", fill = "purple") +
  labs(title = "Delivery Person Ratings by Weather Conditions", 
       x = "Weather Conditions", y = "Average Ratings")



food_delivery_data <- food_delivery_data %>%
  mutate(Age_Group = cut(Delivery_person_Age, breaks = c(20, 25, 30, 35, 40, 45), 
                         labels = c("20-25", "25-30", "30-35", "35-40", "40-45")))

ggplot(food_delivery_data, aes(x = Age_Group, y = Delivery_person_Ratings)) +
  geom_boxplot(fill = "Yellow") +
  labs(title = "Delivery Ratings by Age Group", x = "Age Group",y="Ratings")



