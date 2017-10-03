import re
import logging
import json
import sys
import logging
import traceback

from pyvirtualdisplay import Display
from bs4 import BeautifulSoup
from selenium import webdriver

logging.basicConfig(format='%(asctime)s %(levelname)s: %(message)s', level=logging.INFO,
                    filename='parse_catalogue_log.txt', filemode='w')

def uncaught_exceptions_handler(type, value, tb):
    logging.critical("{0} {1}\n{2}".format(type, value, traceback.format_tb(tb)))
    sys.exit(1)

sys.excepthook = uncaught_exceptions_handler

def main():
    with Display():
        driver = webdriver.Firefox()
        driver.get('http://steamkeys.ovh/?key=7f35e0db30daeb9c4dc813b641f7f0cf')
        html = driver.page_source
        driver.quit()
    soup = BeautifulSoup(html, "html.parser")
    catalogue_items = get_catalogue(soup)

    with open('catalogue.json', 'w', encoding='utf-8') as f:
        json.dump(catalogue_items, f)


def get_catalogue(soup_obj):
    rows = soup_obj.select("table.ttt.dataTable.no-footer")[0].find_all('tr')
    catalogue_items = {}
    for row in rows[1:]:
        tds = row.find_all("td")
        market_link = row.td.a['href'].replace('price_desc', 'price_asc')
        appid = re.search(r'.+/app/(\d+)/', tds[1].a['href']).group(1)
        game_name = tds[1].text
        price = float(tds[2].a.text)
        try:
            store = re.search(r"(https?:\/\/.+?)\/", tds[2].a["href"]).group(1)
        except AttributeError as err:
            store = tds[2].a["href"]
        amount = tds[2].span.text
        try:
            lequeshop_id = re.search(r'type=(\d+)\&', tds[2].find(class_='qckp')['href']).group(1)
        except (AttributeError, TypeError) as err:
            logging.error("Couldn't identify lequeshop product %s", tds[2])
            lequeshop_id = None
        try:
            amount = int(amount)
        except ValueError:
            pass

        catalogue_items[appid] = {
            'store': store,
            'price': price,
            'amount': amount,
            'game_name': game_name,
            'lequeshop_id': lequeshop_id,
            'market_link': market_link
        }
    return catalogue_items


main()
