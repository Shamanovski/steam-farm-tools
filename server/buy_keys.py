import sys
import shelve
import re
import math
import os

import requests
from bs4 import BeautifulSoup

from steampy.client import SteamClient
from payments import WmPayment

wm = WmPayment('database')
steam_client = SteamClient()
steam_client.login('immunepzw', '', {'shared_secret': '='})


def main():
    dedicated_server, budget = sys.argv[1:]
    budget = int(budget)
    catalogue_items = get_catalogue()
    keys_to_purchase = count_keys(dedicated_server, catalogue_items)
    buy_keys(catalogue_items, keys_to_purchase, budget)


def get_catalogue():
    with open("catalogue.html", "r", encoding="utf-8") as f:
        html = f.read()

    games_instock = set()
    for order in os.listdir('keys'):
        with open(os.path.join('keys', order), 'r') as f:
            games_instock.add(f.readline().strip())

    s = BeautifulSoup(html, "html.parser")
    rows = s.select("table.ttt.dataTable.no-footer")[0].find_all('tr')
    catalogue_items = {}
    for row in rows[1:]:
        steam_link = row.td.a['href'].replace('price_desc', 'price_asc')
        tds = row.find_all("td")
        game_name = tds[1].text
        if game_name in games_instock:
            continue
        items = []
        for td in tds[2:7]:
            try:
                price = float(td.text)
            except ValueError:
                continue
            if price >= 2:
                continue
            store = re.search(r"(https?://.+?)(?=/)", td.a["href"]).group(1)
            items.append((store, price, steam_link))
        if items:
            catalogue_items[game_name] = items
    return catalogue_items


def count_keys(dedicated_server, catalogue_items):
    """Count amount of keys for each game"""
    db = shelve.open('database/accounts_games.db')
    path = "accounts_json/%s" % dedicated_server
    account_names = [name.replace('.json', '') for name in os.listdir(path)]
    keys_to_purchase = {}
    for game_name in catalogue_items:
        ctr = 0
        for account_name in account_names:
            already_purchased_games = db.get(account_name, [])
            if game_name in already_purchased_games:
                continue
            ctr += 1
        if ctr:
            keys_to_purchase[game_name] = ctr
    db.close()
    return keys_to_purchase


def buy_keys(catalogue_items, keys_to_purchase, budget):
    expense = 0
    non_leque_shops = set()
    for game_name, amount in keys_to_purchase.items():
        offers = catalogue_items[game_name]
        for store, catalogue_price, steam_link in offers:
            if store in non_leque_shops:
                continue
            while True:
                resp = requests.get(store)
                if '502 Bad Gateway' not in resp.text:
                    break
            if 'leque' not in resp.text:
                non_leque_shops.add(store)
                continue
            s = BeautifulSoup(resp.text, "html.parser")
            product_id = fetch_product_id(s, store, game_name)
            if not product_id:
                continue
            purse, total_price, invoice = get_payment_data(store, product_id, amount)
            if not invoice:
                continue
            cards_price = eval_cards_price(steam_link)
            cost_price = total_price / amount
            sale_price = cards_price * 0.87 * 0.8
            if cost_price / sale_price > 0.8:
                print("The cost price is 0.8 or more as many as the sale one", store, game_name)
                continue
            keys = make_purchase(store, purse, total_price, invoice, game_name)
            save_keys(invoice, game_name, keys)
            expense += total_price
            if expense >= budget:
                print("No budget left")
                return
            break

    print("Non leque shops:", len(non_leque_shops))


def eval_cards_price(url):
    def fetch_card_price(item):
        return float(re.search('[,\d]+', item.span.text).group().replace(',', '.'))

    resp = steam_client.session.get(url)
    s = BeautifulSoup(resp.text, "html.parser")
    package_price = 0
    selector = s.select('span.market_table_value.normal_price')
    for item in sorted(selector[:math.ceil(len(selector) / 2)], key=fetch_card_price):
        card_price = fetch_card_price(item)
        package_price += card_price

    return package_price


def fetch_product_id(s, store, game_name):
    if store == 'http://steamkeystore.ru':
        game_name += ' [steam key]'
    elif store == 'http://steamrandomkeys.ru':
        game_name += ' [ Steam key ]'
    try:
        product_link = s.find(text=game_name).find_parent('a')['href']
    except (AttributeError, TypeError) as err:
        print(err, store, game_name)
        return None
    try:
        product_id = re.search(r'/goods/info/(\d+)', product_link).group(1)
    except AttributeError as err:
        print(err, product_link, store, game_name)
        return None

    return product_id


def get_payment_data(store, product_id, amount):
    data = {
        "email": "jamjut1995@gmail.com",
        "fund": "1",
        "copupon": "",
        "type": product_id,
        "count": amount
    }
    resp = requests.post("%s/order/" % store, data=data).json()
    if resp.get("error", None):
        print(resp)
        return None, None, None
    if "TRUE" not in resp["ok"]:
        print(resp)
        raise Exception
    try:
        total_price = float(resp["price"].replace(" WMR", ""))
    except KeyError as err:
        print(err, resp, store)
        raise err
    # if total_price / amount > 2:
    #     print("The actual price is more that the one from the catalogue:", total_price / amount)
    #     return None, None, None
    purse = resp["fund"].strip("<\/b>")
    invoice = resp["bill"].strip("<\/b>")

    return purse, total_price, invoice


def make_purchase(store, purse, total_price, invoice, game_name):
    print(store, purse, total_price, invoice, game_name)
    resp = wm.init_payment(purse, total_price, invoice)
    if resp['retval'] != '0':
        print(resp)
        raise Exception
    order = re.search(r'\[(.+)\]', invoice).group(1)
    session = requests.Session()
    resp = session.get("%s/order/%s" % (store, order))
    if 'Проверка покупателя' not in resp.text:
        if re.search(r'\w{5}-\w{5}-\w{5}', resp.text):
            return resp.text

    regexr = r'\w{5}-\w{5}-\w{5}'
    resp = session.post("%s/order/get/%s/saved/" % (store, order),
                        data={"email": "jamjut1995@gmail.com"})
    if 'Номер заказа' not in resp.text and not re.search(regexr, resp.text):
        print(resp.text)
        raise Exception

    resp = session.get("%s/order/get/%s/saved/" % (store, order))
    if not re.search(regexr, resp.text):
        print(resp.text)
        raise Exception

    return resp.text


def save_keys(invoice, game_name, keys):
    with open('keys/%s.txt' % invoice, "w", encoding="utf-8") as f:
        f.write(game_name + '\n')
        f.write(keys.replace('\r', ''))

main()
