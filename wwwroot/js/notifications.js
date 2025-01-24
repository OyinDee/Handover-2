"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveNotification", function (message) {
    const notificationList = document.getElementById("notificationList");
    const notificationItem = document.createElement("li");
    notificationItem.textContent = message;
    notificationItem.className = "list-group-item list-group-item-info";
    notificationList.appendChild(notificationItem);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});
