"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveNotification", function (message) {
    // Create notification element
    const notificationList = document.getElementById("notificationList");
    const notificationItem = document.createElement("li");
    notificationItem.textContent = message;
    notificationItem.className = "list-group-item list-group-item-info";
    
    // Add timestamp
    const timestamp = document.createElement("small");
    timestamp.textContent = new Date().toLocaleTimeString();
    timestamp.className = "float-end text-muted";
    notificationItem.appendChild(timestamp);
    
    // Add to list
    notificationList.insertBefore(notificationItem, notificationList.firstChild);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});
