CREATE OR REPLACE FUNCTION add_seats()
RETURNS TRIGGER
LANGUAGE 'plpgsql'
AS $$
DECLARE
    row_counter INTEGER = 1;
    seat_counter INTEGER = 1;
BEGIN
    WHILE row_counter <= NEW."Rows" LOOP
        WHILE seat_counter <= NEW."SeatsPerRow" LOOP
            INSERT INTO "Seat" ("HallId", "TypeId", "Row", "Number") VALUES (NEW."Id", 1, row_counter, seat_counter);
            seat_counter = seat_counter + 1;
        END LOOP;
        row_counter = row_counter + 1;
        seat_counter = 1;
    END LOOP;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS add_seats_tg on "Hall";
CREATE TRIGGER add_seats_tg 
AFTER INSERT 
ON "Hall"
FOR EACH ROW
EXECUTE PROCEDURE add_seats();
