Ship your HomeSeer events to Elasticsearch!
[![Build Status](https://travis-ci.org/legrego/HSPI_Elasticsearch.svg?branch=master)](https://travis-ci.org/legrego/HSPI_Elasticsearch)
=====

A C# Plugin for HomeSeer installations which publishes events to your [Elasticsearch](https://elastic.co) cluster.


## Features
* Effeciently publishes HomeSeer events to Elasticsearch using the Bulk API
* Automatically maintains Indexes and Index Templates using the Rollover API
* Supports [X-Pack Security](https://www.elastic.co/products/x-pack/security) via optional username and password

## Compatability
* HomeSeer 3 (Linux and Windows are both supported)
* Elasticsearch 6.x (Self or [Cloud](https://www.elastic.co/cloud) hosted), with or without [X-Pack](https://www.elastic.co/products/x-pack).

## Support
This project is not endorsed or supported by either Elastic or HomeSeer - please open a GitHub issue for any questions, bugs, or feature requests.

## Acknowledgements
* Nicolai Peteri (@NicolaiPetri) for his [HS3_EnOcean_Plugin](https://github.com/NicolaiPetri/HS3_EnOcean_Plugin), which gave me a working example for subscribing plugins to HomeSeer events.
