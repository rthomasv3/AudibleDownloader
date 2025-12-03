import { createApp } from "vue";
import router from "./router";
import PrimeVue from "primevue/config";
import { definePreset } from "@primeuix/themes";
import Aura from "@primeuix/themes/aura";
import ConfirmationService from "primevue/confirmationservice";
import "./style.css";
import App from "./App.vue";

const app = createApp(App);

const IndigoAura = definePreset(Aura, {
    semantic: {
        primary: {
            50: "{indigo.50}",
            100: "{indigo.100}",
            200: "{indigo.200}",
            300: "{indigo.300}",
            400: "{indigo.400}",
            500: "{indigo.500}",
            600: "{indigo.600}",
            700: "{indigo.700}",
            800: "{indigo.800}",
            900: "{indigo.900}",
            950: "{indigo.950}",
        },
    },
});

app.use(PrimeVue, {
    theme: {
        preset: IndigoAura,
        options: {
            darkModeSelector: ".dark-mode",
        },
    },
});

app.use(router);
app.use(ConfirmationService);

app.mount("#app");
