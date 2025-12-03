<script setup>
import { onMounted } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

onMounted(() => {
    let retryCount = 0;
    let maxRetries = 30;
    let interval = setInterval(async () => {
        const loginStatusResult = await galdrInvoke("getLoginStatus");
        if (loginStatusResult.isLoggedIn) {
            clearInterval(interval);
            await router.push("/");
        } else if (retryCount++ >= maxRetries) {
            clearInterval(interval);
            await router.push("/login");
        }
    }, 1000);
});
</script>

<template>
    <div class="flex h-[32rem] items-center">
        <ProgressSpinner />
    </div>
</template>

<style scoped></style>
