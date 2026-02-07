from pyspark import pipelines as dp
from pyspark.sql.functions import *

@dp.table
def flight_stats():
    df = spark.read.table("ingest_flights")
    return (
        df.agg(
            count("*").alias("num_events"),
            countDistinct("icao24").alias("distinct_aircraft"),
            max("velocity").alias("max_velocity"),
        )
    )