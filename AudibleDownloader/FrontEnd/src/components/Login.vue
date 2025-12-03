<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const useBrowserLogin = ref(false);
const loginUrl = ref();
const oauthErrorUrl = ref();
const error = ref();

async function getLoginUrl() {
    const urlResult = await galdrInvoke("getOAuthUrl");
    return urlResult?.url;
}

async function loginHere() {
    const url = await getLoginUrl();
    if (url) {
        window.location.href = url;
    }
}

async function browserLogin() {
    const url = await getLoginUrl();
    if (url) {
        loginUrl.value = url;
        useBrowserLogin.value = true;
    }
}

async function submitBrowserLogin() {
    error.value = null;
    if (oauthErrorUrl.value.includes('openid.oa2.authorization_code=')) {
        galdrInvoke('onAuthorizationCodeFound', { url: oauthErrorUrl.value });
        await router.push("/loading");
    } else {
        error.value = "Invalid URL: authorization_code not found.";
    }
}
</script>

<template>
    <div class="flex flex-1 px-4">
        <div class="flex flex-1 flex-col mx-auto max-w-6xl m-z-auto items-center py-12 gap-5">
            <p class="text-4xl mb-2">Login to Audible</p>

            <div v-if="error">
                <Message severity="error">{{ error }}</Message>
            </div>

            <div v-if="useBrowserLogin" class="flex flex-col max-w-3xl text-center">
                <p>
                    Click <a :href="loginUrl" target="_blank">here</a> to login using your browser.
                </p>
                <p>
                    Before entering your credentials into any site, verify the URL is as expected.
                </p>
                <p>
                    In this case, it should be Amazon's official website at https://www.amazon.com/
                </p>

                <p class="mt-3">
                    After you log in, you'll be forwarded to an Error page. <strong>This is expected.</strong>
                </p>

                <p>
                    Copy the URL from the error page and paste it below:
                </p>

                <Textarea v-model="oauthErrorUrl" class="mt-5 resize-none" rows="5" cols="70" inputmode="url"
                    placeholder="Paste error page URL here..." />

                <Button label="Submit" class="mt-5" @click="submitBrowserLogin" />

            </div>
            <div v-else class="flex w-65 flex-col gap-3">
                <Button label="Login Here" @click="loginHere" />

                <div class="flex items-center gap-3">
                    <Divider />
                    <p>or</p>
                    <Divider />
                </div>

                <Button label="Login Using Browser" @click="browserLogin" />
            </div>
        </div>
    </div>
</template>

<style scoped></style>
