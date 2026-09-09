DROP TABLE IF EXISTS colors;
CREATE TABLE colors (
  id INTEGER NOT NULL PRIMARY KEY,
  name TEXT NOT NULL UNIQUE
) STRICT;

INSERT INTO colors (id, name) VALUES 
(0, 'AF500'),
(1, 'F100'),
(2, 'F201'),
(3, 'F301'),
(4, 'F401'),
(5, 'F9006'),
(6, 'G101'),
(7, 'G103');

DROP TABLE IF EXISTS orders;
CREATE TABLE orders (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  date TEXT NOT NULL
) STRICT;

DROP TABLE IF EXISTS panels;
CREATE TABLE panels (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  length INTEGER NOT NULL,
  width TEXT NOT NULL,
  color_id INTEGER NOT NULL,
  storage_count INTEGER NOT NULL,
  UNIQUE (id, color_id),
  UNIQUE (length, width, color_id),
  FOREIGN KEY (color_id) REFERENCES colors (id),
  CONSTRAINT panels_chk_1 CHECK (length > 0 AND length <= 13200),
  CONSTRAINT panels_chk_2 CHECK (storage_count >= 0),
  CONSTRAINT panels_chk_width CHECK (width IN ('500mm', '610mm'))
) STRICT;

DROP TABLE IF EXISTS configurations;
CREATE TABLE configurations (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  position INTEGER NOT NULL,
  order_id INTEGER NOT NULL,
  panel_id INTEGER NOT NULL,
  color_id INTEGER NOT NULL,
  UNIQUE (id, order_id, color_id),
  FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE,
  FOREIGN KEY (panel_id, color_id) REFERENCES panels (id, color_id)
) STRICT;

DROP TABLE IF EXISTS kapuk;
CREATE TABLE kapuk (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  length INTEGER NOT NULL,
  height INTEGER NOT NULL,
  serial_number TEXT NOT NULL UNIQUE,
  position INTEGER NOT NULL,
  color_id INTEGER NOT NULL,
  order_id INTEGER NOT NULL,
  UNIQUE (id, order_id, color_id),
  FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE,
  FOREIGN KEY (color_id) REFERENCES colors (id),
  CONSTRAINT kapuk_chk_1 CHECK (length > 0 AND length <= 13200),
  CONSTRAINT kapuk_chk_2 CHECK (height > 0 AND height <= 5210)
) STRICT;

DROP TABLE IF EXISTS configuration_items;
CREATE TABLE configuration_items (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  position INTEGER NOT NULL,
  kapu_id INTEGER NOT NULL,
  configuration_id INTEGER NOT NULL,
  count INTEGER NOT NULL,
  order_id INTEGER NOT NULL,
  color_id INTEGER NOT NULL,
  UNIQUE (kapu_id, configuration_id),
  FOREIGN KEY (kapu_id, order_id, color_id) REFERENCES kapuk (id, order_id, color_id) ON DELETE CASCADE,
  FOREIGN KEY (configuration_id, order_id, color_id) REFERENCES configurations (id, order_id, color_id) ON DELETE CASCADE,
  CONSTRAINT configuration_items_chk_1 CHECK (count > 0)
) STRICT;

DROP TABLE IF EXISTS storage_edits;
CREATE TABLE storage_edits (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  date TEXT NOT NULL,
  message TEXT NOT NULL
) STRICT;

DROP TABLE IF EXISTS storage_edit_items;
CREATE TABLE storage_edit_items (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  position INTEGER NOT NULL,
  panel_id INTEGER NOT NULL,
  edit_id INTEGER NOT NULL,
  delta_count INTEGER NOT NULL,
  UNIQUE (panel_id, edit_id),
  FOREIGN KEY (panel_id) REFERENCES panels (id),
  FOREIGN KEY (edit_id) REFERENCES storage_edits (id),
  CONSTRAINT storage_edit_items_chk_1 CHECK (delta_count <> 0)
) STRICT;