import http from 'k6/http';
import { sleep } from 'k6';
import { v4 as uuidv4 } from './uuid.js';


const products =
    Array
        .from(Array(100).keys())
        .map(i => ({
            product: {
                id: i.toString(),
                name: `Product ${i}`,
                price: 100 + i,
            }
        }));

const cartIds = 
    Array
        .from(Array(100).keys())
        .map(i => uuidv4());

const productIds = products.map(p => p.product.id);

const params = {
    headers: {
        'Content-Type': 'application/json',
    },
};
export function setup() {
    products.map(product => {
        http.post('http://localhost:5000/product', JSON.stringify(product), params);
    });
}

export default function () {
    const productId = productIds[Math.floor(Math.random()*productIds.length)];
    // const cartId = cartIds[Math.floor(Math.random()*cartIds.length)];
    http.post('http://localhost:5000/cart', JSON.stringify({
        "cartId": uuidv4(), //cartId.toString(),
        "productId": productId,
        "quantity": 1
    }), params);
}
