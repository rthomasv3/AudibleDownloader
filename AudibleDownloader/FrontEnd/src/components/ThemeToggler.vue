<script setup>
import { Icon } from "@iconify/vue";
import { ref, onMounted } from "vue";

const isDarkMode = ref(true);

onMounted(() => {
    setDefaultTheme();
});

function setDefaultTheme() {
    const theme = localStorage.getItem("theme");

    if (theme) {
        if (theme === "light") {
            document.documentElement.classList.remove("dark-mode");
            isDarkMode.value = false;
        }
    } else {
        const prefersLight = window.matchMedia("(prefers-color-scheme: light)");
        if (prefersLight.matches) {
            document.documentElement.classList.remove("dark-mode");
            mode.value = false;
        }
    }
}

function toggleTheme() {
    isDarkMode.value = !isDarkMode.value;
    document.documentElement.classList.toggle("dark-mode");
    localStorage.setItem("theme", document.documentElement.classList.contains("dark-mode") ? "dark" : "light");
}
</script>

<template>
    <div>
        <ToggleSwitch class="mt-2" :defaultValue="isDarkMode" @change="toggleTheme">
            <template #handle="{ checked }">
                <Icon icon="material-symbols:dark-mode-outline-rounded" v-if="checked" />
                <Icon icon="material-symbols-light:sunny-outline-rounded" v-else />
            </template>
        </ToggleSwitch>
    </div>
</template>

<style scoped></style>