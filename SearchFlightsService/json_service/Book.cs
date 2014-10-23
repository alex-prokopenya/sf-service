using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;
using Jayrock.Json.Conversion;

/// <summary>
/// Summary description for Book
/// </summary>

namespace SearchFlightsService
{
    public class Book
    {
        /*
        Описание полей:
        book_id – Идентификатор брони. Строка.
        mark – Содержимое метки mark в урле, по которому перешел пользователь, создавший бронь (см. описание поля url в ответе на запросы типа check_availability). Строка.
        passengers – Список из 4 целых чисел [adults, children, infants with seat, infants without seat], содержащий количество взрослых пассажиров, детей, младенцев с местом и младенцев без места в брони.
        price – Суммарная цена билетов брони в рублях. Целое число.
        created – Время создания брони. Список из 6 целых чисел [year, month, day, hour, minute, second]. Время должно быть в таймзоне UTC.
        route – Маршрут забронированных билетов (см. ответ на запрос типа search_tickets).
        status – Статус брони. Одно из трех значений: "paid" (бронь уже оплачена), "reserved" (бронь не оплачена, но еще не снята) или "expired" (время оплаты прошло и бронь не была оплачена).
         */
        public Book()
        {
            JsonArray jar = new JsonArray();

            //jar = JsonConvert.Import();
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jArr = new JsonArray();

            jArr.Add(this.BookId);
            jArr.Add(this.Mark);

            JsonArray pass = new JsonArray();
            foreach (int item in this.Passengers)
                pass.Add(item);

            jArr.Add(pass);

            jArr.Add(this.Price);

            JsonArray dateCreated = new JsonArray();
            foreach (int item in this.Created)
                dateCreated.Add(item);

            jArr.Add(dateCreated);
            jArr.Add(this.Route);
            jArr.Add(this.Status);

            return jArr;
        }

        public string BookId = "";
        public string Mark = "";
        public int[] Passengers = null;
        public int Price = 0;
        public int[] Created = null;
        public JsonArray Route = null;
        public string Status = "";
    }
}