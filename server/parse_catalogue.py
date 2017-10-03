import re
import logging
import json
import collections
import sys
import io
import urllib3

import requests
from bs4 import BeautifulSoup
from selenium import webdriver


sys.stdout = io.TextIOWrapper(sys.stdout.detach(), encoding = 'utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.detach(), encoding = 'utf-8')


def main():
    driver = webdriver.PhantomJS('server/phantomjs', service_log_path='phantomjs.log')
    driver.get('http://steamkeys.ovh/?key=7f35e0db30daeb9c4dc813b641f7f0cf')
    html = driver.page_source
    driver.quit()
    with open('bla.html', 'w', encoding='utf-8') as f:
        f.write(html)
    soup = BeautifulSoup(html, "html.parser")
    catalogue_items = get_catalogue(soup)

    with open('catalogue.json', 'w', encoding='utf-8') as f:
        json.dump(catalogue_items, f)


def get_catalogue(soup_obj):
    rows = soup_obj.select("table.ttt.dataTable.no-footer")[0].find_all('tr')
    catalogue_items = {}
    for row in rows[1:]:
        tds = row.find_all("td")
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
            print(err)
            lequeshop_id = None
        try:
            amount = int(amount)
        except ValueError:
            pass
        catalogue_items[appid] = {'store': store, 'price': price, 'amount': amount, 'game_name': game_name, 'lequeshop_id': lequeshop_id}
    return catalogue_items


main()
