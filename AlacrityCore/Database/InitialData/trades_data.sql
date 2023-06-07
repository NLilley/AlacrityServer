INSERT INTO trades
  (trade_id, instrument_id, client_id, order_id, trade_date, trade_direction, quantity, price, profit)
VALUES
  (1, 4, 1, 1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), 1, 5, 300, null),
  (2, 1, 1, 2, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), 1, 1000, 100, null),
  (3, 2, 2, 3, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), 1, 2000, 200, null);