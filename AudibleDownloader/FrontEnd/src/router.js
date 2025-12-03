import { nextTick } from "vue";
import { createWebHistory, createRouter } from "vue-router";

const Library = () => import("./components/Library.vue");

const NotFound = () => import("./components/NotFound.vue");

const routes = [
    { path: "/", component: Library },
    { path: "/:pathMatch(.*)*", component: NotFound },
];

const router = createRouter({
    history: createWebHistory(),
    routes,
    scrollBehavior(to, from, savedPosition) {
        return new Promise((resolve) => {
            nextTick(() => {
                const images = document.querySelectorAll("img");
                const promises = Array.from(images).map((img) => (img.complete ? Promise.resolve() : new Promise((resolve) => (img.onload = resolve))));
                Promise.all(promises).then(() => resolve({ top: 0 }));
            });
        });
    },
});

export default router;
