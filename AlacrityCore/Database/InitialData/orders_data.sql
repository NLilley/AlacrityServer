INSERT INTO ORDERS
  (order_id, instrument_id, client_id, order_date, order_kind, order_status, order_direction, limit_price, quantity, filled)
VALUES
  (1, 4, 1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), 2, 2, 1, 300, 5, 5),
  (2, 1, 1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), 2, 2, 1, 100, 1000, 1000),
  (3, 2, 2, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), 2, 2, 1, 200, 2000, 2000);