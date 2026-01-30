import getOfflineAudioContext from '@creepjs/audio'
import getCanvas2d from '@creepjs/canvas'
import getCSS from '@creepjs/css'
import getCSSMedia from '@creepjs/cssmedia'
import getHTMLElementVersion from '@creepjs/document'
import getClientRects from '@creepjs/domrect'
import getConsoleErrors from '@creepjs/engine'
import { caniuse, getCapturedErrors } from '@creepjs/errors'
import getEngineFeatures from '@creepjs/features'
import getFonts from '@creepjs/fonts'
import getHeadlessFeatures from '@creepjs/headless'
import getIntl from '@creepjs/intl'
import { getLies } from '@creepjs/lies'
import getMaths from '@creepjs/math'
import getMedia from '@creepjs/media'
import getNavigator from '@creepjs/navigator'
import getResistance from '@creepjs/resistance'
import getScreen from '@creepjs/screen'
import getVoices from '@creepjs/speech'
import getSVG from '@creepjs/svg'
import getTimezone from '@creepjs/timezone'
import { getTrash } from '@creepjs/trash'
import { hashify } from '@creepjs/utils/crypto'
import { IS_BLINK, LowerEntropy, braveBrowser, getBraveMode, getBraveUnprotectedParameters } from '@creepjs/utils/helpers'
import getCanvasWebgl from '@creepjs/webgl'
import getWindowFeatures from '@creepjs/window'
import getBestWorkerScope from '@creepjs/worker'

export const getFingerprint = async (): Promise<string> => {
    const isBrave = IS_BLINK ? await braveBrowser() : false
    const braveMode: any = isBrave ? getBraveMode() : {}
    const braveFingerprintingBlocking = isBrave && (braveMode.standard || braveMode.strict)

    // @ts-ignore
    const [
        workerScopeComputed,
        voicesComputed,
        offlineAudioContextComputed,
        canvasWebglComputed,
        canvas2dComputed,
        windowFeaturesComputed,
        htmlElementVersionComputed,
        cssComputed,
        cssMediaComputed,
        screenComputed,
        mathsComputed,
        consoleErrorsComputed,
        timezoneComputed,
        clientRectsComputed,
        fontsComputed,
        mediaComputed,
        svgComputed,
        resistanceComputed,
        intlComputed,
    ] = await Promise.all([
        getBestWorkerScope(),
        getVoices(),
        getOfflineAudioContext(),
        getCanvasWebgl(),
        getCanvas2d(),
        getWindowFeatures(),
        getHTMLElementVersion(),
        getCSS(),
        getCSSMedia(),
        getScreen(),
        getMaths(),
        getConsoleErrors(),
        getTimezone(),
        getClientRects(),
        getFonts(),
        getMedia(),
        getSVG(),
        getResistance(),
        getIntl(),
    ]).catch((error) => console.error(error.message))

    const navigatorComputed = await getNavigator(workerScopeComputed)
        .catch((error: any) => console.error(error.message))

    // @ts-ignore
    const [
        headlessComputed,
        featuresComputed,
    ] = await Promise.all([
        getHeadlessFeatures({
            webgl: canvasWebglComputed,
            workerScope: workerScopeComputed,
        }),
        getEngineFeatures({
            cssComputed,
            navigatorComputed,
            windowFeaturesComputed,
        }),
    ]).catch((error) => console.error(error.message))

    // @ts-ignore
    const [
        liesComputed,
        trashComputed,
        capturedErrorsComputed,
    ] = await Promise.all([
        getLies(),
        getTrash(),
        getCapturedErrors(),
    ]).catch((error) => console.error(error.message))

    // GPU Prediction
    const { parameters: gpuParameter } = canvasWebglComputed || {}
    const reducedGPUParameters = {
        ...(
            braveFingerprintingBlocking ? getBraveUnprotectedParameters(gpuParameter) :
                gpuParameter
        ),
        RENDERER: undefined,
        SHADING_LANGUAGE_VERSION: undefined,
        UNMASKED_RENDERER_WEBGL: undefined,
        UNMASKED_VENDOR_WEBGL: undefined,
        VERSION: undefined,
        VENDOR: undefined,
    }

    const hardenEntropy = (workerScope: any, prop: any) => {
        return (
            !workerScope ? prop :
                (workerScope.localeEntropyIsTrusty && workerScope.localeIntlEntropyIsTrusty) ? prop :
                    undefined
        )
    }

    const fp = {
        workerScope: workerScopeComputed,
        navigator: navigatorComputed,
        windowFeatures: windowFeaturesComputed,
        headless: headlessComputed,
        htmlElementVersion: htmlElementVersionComputed,
        cssMedia: cssMediaComputed,
        css: cssComputed,
        screen: screenComputed,
        voices: voicesComputed,
        media: mediaComputed,
        canvas2d: canvas2dComputed,
        canvasWebgl: canvasWebglComputed,
        maths: mathsComputed,
        consoleErrors: consoleErrorsComputed,
        timezone: timezoneComputed,
        clientRects: clientRectsComputed,
        offlineAudioContext: offlineAudioContextComputed,
        fonts: fontsComputed,
        lies: liesComputed,
        trash: trashComputed,
        capturedErrors: capturedErrorsComputed,
        svg: svgComputed,
        resistance: resistanceComputed,
        intl: intlComputed,
        features: featuresComputed,
    }

    // Construct the "creep" object which represents the stable fingerprint
    const privacyResistFingerprinting = (
        // @ts-ignore
        fp.resistance && /^(tor browser|firefox)$/i.test(fp.resistance.privacy)
    )

    // harden gpu
    const hardenGPU = (canvasWebgl: any) => {
        if (!canvasWebgl || !canvasWebgl.gpu) {
            return {}
        }
        const { gpu: { confidence, compressedGPU } } = canvasWebgl
        return (
            confidence == 'low' ? {} : {
                UNMASKED_RENDERER_WEBGL: compressedGPU,
                UNMASKED_VENDOR_WEBGL: (canvasWebgl.parameters || {}).UNMASKED_VENDOR_WEBGL,
            }
        )
    }

    const creep = {
        navigator: (
            // @ts-ignore
            !fp.navigator || fp.navigator.lied ? undefined : {
                // @ts-ignore
                bluetoothAvailability: fp.navigator.bluetoothAvailability,
                // @ts-ignore
                device: fp.navigator.device,
                // @ts-ignore
                deviceMemory: fp.navigator.deviceMemory,
                // @ts-ignore
                hardwareConcurrency: fp.navigator.hardwareConcurrency,
                // @ts-ignore
                maxTouchPoints: fp.navigator.maxTouchPoints,
                // @ts-ignore
                oscpu: fp.navigator.oscpu,
                // @ts-ignore
                platform: fp.navigator.platform,
                // @ts-ignore
                system: fp.navigator.system,
                userAgentData: {
                    // @ts-ignore
                    ...(fp.navigator.userAgentData || {}),
                    // loose
                    brandsVersion: undefined,
                    uaFullVersion: undefined,
                },
                // @ts-ignore
                vendor: fp.navigator.vendor,
            }
        ),
        screen: (
            // @ts-ignore
            !fp.screen || fp.screen.lied || privacyResistFingerprinting || LowerEntropy.SCREEN ? undefined :
                hardenEntropy(
                    fp.workerScope, {
                    // @ts-ignore
                    height: fp.screen.height,
                    // @ts-ignore
                    width: fp.screen.width,
                    // @ts-ignore
                    pixelDepth: fp.screen.pixelDepth,
                    // @ts-ignore
                    colorDepth: fp.screen.colorDepth,
                    // @ts-ignore
                    lied: fp.screen.lied,
                },
                )
        ),
        workerScope: !fp.workerScope || fp.workerScope.lied ? undefined : {
            deviceMemory: (
                braveFingerprintingBlocking ? undefined : fp.workerScope.deviceMemory
            ),
            hardwareConcurrency: (
                braveFingerprintingBlocking ? undefined : fp.workerScope.hardwareConcurrency
            ),
            // system locale in blink
            language: !LowerEntropy.TIME_ZONE ? fp.workerScope.language : undefined,
            platform: fp.workerScope.platform,
            system: fp.workerScope.system,
            device: fp.workerScope.device,
            timezoneLocation: (
                !LowerEntropy.TIME_ZONE ?
                    hardenEntropy(fp.workerScope, fp.workerScope.timezoneLocation) :
                    undefined
            ),
            webglRenderer: (
                (fp.workerScope && fp.workerScope.gpu && fp.workerScope.gpu.confidence != 'low') ? fp.workerScope.gpu.compressedGPU : undefined
            ),
            webglVendor: (
                (fp.workerScope && fp.workerScope.gpu && fp.workerScope.gpu.confidence != 'low') ? fp.workerScope.webglVendor : undefined
            ),
            userAgentData: (fp.workerScope ? {
                ...fp.workerScope.userAgentData,
                // loose
                brandsVersion: undefined,
                uaFullVersion: undefined,
            } : undefined),
        },
        media: fp.media,
        canvas2d: ((canvas2d: any) => {
            if (!canvas2d) {
                return
            }
            const { lied, liedTextMetrics } = canvas2d
            let data
            if (!lied) {
                const { dataURI, paintURI, textURI, emojiURI } = canvas2d
                data = {
                    lied,
                    ...{ dataURI, paintURI, textURI, emojiURI },
                }
            }
            if (!liedTextMetrics) {
                const { textMetricsSystemSum, emojiSet } = canvas2d
                data = {
                    ...(data || {}),
                    ...{ textMetricsSystemSum, emojiSet },
                }
            }
            return data
        })(fp.canvas2d),
        canvasWebgl: (!fp.canvasWebgl || fp.canvasWebgl.lied || LowerEntropy.WEBGL) ? undefined : (
            braveFingerprintingBlocking ? {
                parameters: {
                    ...getBraveUnprotectedParameters(fp.canvasWebgl.parameters),
                    ...hardenGPU(fp.canvasWebgl),
                },
            } : {
                ...((gl: any, canvas2d: any) => {
                    if ((canvas2d && canvas2d.lied) || LowerEntropy.CANVAS) {
                        // distrust images
                        const { extensions, gpu, lied, parameterOrExtensionLie } = gl
                        return {
                            extensions,
                            gpu,
                            lied,
                            parameterOrExtensionLie,
                        }
                    }
                    return gl
                })(fp.canvasWebgl, fp.canvas2d),
                parameters: {
                    ...fp.canvasWebgl.parameters,
                    ...hardenGPU(fp.canvasWebgl),
                },
            }
        ),
        cssMedia: !fp.cssMedia ? undefined : {
            // @ts-ignore
            reducedMotion: caniuse(() => fp.cssMedia.mediaCSS['prefers-reduced-motion']),
            colorScheme: (
                braveFingerprintingBlocking ? undefined :
                    // @ts-ignore
                    caniuse(() => fp.cssMedia.mediaCSS['prefers-color-scheme'])
            ),
            // @ts-ignore
            monochrome: caniuse(() => fp.cssMedia.mediaCSS.monochrome),
            // @ts-ignore
            invertedColors: caniuse(() => fp.cssMedia.mediaCSS['inverted-colors']),
            // @ts-ignore
            forcedColors: caniuse(() => fp.cssMedia.mediaCSS['forced-colors']),
            // @ts-ignore
            anyHover: caniuse(() => fp.cssMedia.mediaCSS['any-hover']),
            // @ts-ignore
            hover: caniuse(() => fp.cssMedia.mediaCSS.hover),
            // @ts-ignore
            anyPointer: caniuse(() => fp.cssMedia.mediaCSS['any-pointer']),
            // @ts-ignore
            pointer: caniuse(() => fp.cssMedia.mediaCSS.pointer),
            // @ts-ignore
            colorGamut: caniuse(() => fp.cssMedia.mediaCSS['color-gamut']),
            screenQuery: (
                privacyResistFingerprinting || (LowerEntropy.SCREEN || LowerEntropy.IFRAME_SCREEN) ?
                    undefined :
                    // @ts-ignore
                    hardenEntropy(fp.workerScope, caniuse(() => fp.cssMedia.screenQuery))
            ),
        },
        // @ts-ignore
        css: !fp.css ? undefined : fp.css.system.fonts,
        timezone: !fp.timezone || fp.timezone.lied || LowerEntropy.TIME_ZONE ? undefined : {
            locationMeasured: hardenEntropy(fp.workerScope, fp.timezone.locationMeasured),
            lied: fp.timezone.lied,
        },
        offlineAudioContext: !fp.offlineAudioContext ? undefined : (
            fp.offlineAudioContext.lied || LowerEntropy.AUDIO ? undefined :
                fp.offlineAudioContext
        ),
        fonts: !fp.fonts || fp.fonts.lied || LowerEntropy.FONTS ? undefined : fp.fonts.fontFaceLoadFonts,
    }

    const creepHash = await hashify(creep)
    return creepHash
}
