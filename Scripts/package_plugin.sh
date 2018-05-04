#!/usr/bin/env bash

mkdir ./buildTemp

cp -r ./bin/Release/ ./buildTemp
cd ./buildTemp/Release
rm Scheduler.dll HSCF.dll HomeSeerAPI.dll ADODB.dll

zip -r ../../HSPI_Elasticsearch.zip .

cd ../../
rm -r ./buildTemp
