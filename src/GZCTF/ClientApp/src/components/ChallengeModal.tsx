import {
  Button,
  Divider,
  Group,
  Modal,
  ModalProps,
  ScrollArea,
  Stack,
  TextInput,
  Text,
  Title,
  useMantineTheme,
  ScrollAreaProps,
  LoadingOverlay,
} from '@mantine/core'
import { mdiLightbulbOnOutline, mdiOpenInNew, mdiPackageVariantClosed } from '@mdi/js'
import Icon from '@mdi/react'
import { FC, forwardRef, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import InstanceEntry from '@Components/InstanceEntry'
import MarkdownRender, { InlineMarkdownRender } from '@Components/MarkdownRender'
import { ChallengeTagItemProps } from '@Utils/Shared'
import { ChallengeDetailModel, ChallengeType } from '@Api'
import classes from '@Styles/ChallengeModal.module.css'

export interface ChallengeModalProps extends ModalProps {
  challenge?: ChallengeDetailModel
  tagData: ChallengeTagItemProps
  solved?: boolean
  disabled?: boolean
  flag: string
  setFlag: (value: string | React.ChangeEvent<any> | null | undefined) => void
  onCreate: () => void
  onExtend: () => void
  onDestroy: () => void
  onSubmitFlag: () => void
  onDownload?: () => void
}

const ChallengeModal: FC<ChallengeModalProps> = (props) => {
  const {
    challenge,
    tagData,
    solved,
    disabled,
    flag,
    setFlag,
    onCreate,
    onExtend,
    onDestroy,
    onDownload,
    onSubmitFlag,
    ...modalProps
  } = props
  const { t } = useTranslation()
  const theme = useMantineTheme()

  const placeholders = t('challenge.content.flag_placeholders', {
    returnObjects: true,
  }) as string[]

  const [placeholder, setPlaceholder] = useState('')

  useEffect(() => {
    setPlaceholder(placeholders[Math.floor(Math.random() * placeholders.length)])
  }, [challenge])

  const isDynamic =
    challenge?.type === ChallengeType.StaticContainer ||
    challenge?.type === ChallengeType.DynamicContainer

  const title = (
    <Stack gap="xs">
      <Group wrap="nowrap" w="100%" justify="space-between" gap="sm">
        <Group wrap="nowrap" gap="sm">
          {tagData && <Icon path={tagData.icon} size={1} color={theme.colors[tagData?.color][5]} />}
          <Title w="calc(100% - 1.5rem)" order={4} lineClamp={1}>
            {challenge?.title ?? ''}
          </Title>
        </Group>
        <Text miw="5em" fw="bold" ff="monospace">
          {challenge?.score ?? 0} pts
        </Text>
      </Group>
      <Divider />
    </Stack>
  )

  const content = (
    <Stack gap="xs" mih="20vh">
      <MarkdownRender source={challenge?.content ?? ''} />
      {challenge?.hints && challenge.hints.length > 0 && (
        <Stack gap={2} pt="sm">
          {challenge.hints.map((hint) => (
            <Group key={hint} gap="xs" align="flex-start" wrap="nowrap">
              <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
              <InlineMarkdownRender key={hint} size="sm" maw="calc(100% - 2rem)" source={hint} />
            </Group>
          ))}
        </Stack>
      )}
    </Stack>
  )

  const withAttachment = !!challenge?.context?.url || onDownload

  const link = challenge?.context?.url
  const local = link && link.startsWith('/assets')

  const attachment = withAttachment && (
    <Group gap="xs" justify="flex-start" align="center" wrap="nowrap">
      <Text fw="bold" size="sm">
        {t('challenge.button.download.attachment')}
      </Text>
      <Text>👉</Text>
      <Button
        component="a"
        href={link ?? '#'}
        variant="light"
        size="compact-sm"
        target="_blank"
        rel="noreferrer"
        leftSection={<Icon path={local ? mdiPackageVariantClosed : mdiOpenInNew} size={0.8} />}
        maw="20rem"
        onClick={
          onDownload &&
          ((e: any) => {
            e.preventDefault()
            onDownload()
          })
        }
      >
        {local ? link.split('/').pop() : t('common.content.external_link')}
      </Button>
    </Group>
  )

  const withInstance = isDynamic && challenge?.context

  const instance = withInstance && (
    <InstanceEntry
      context={challenge.context!}
      onCreate={onCreate}
      onExtend={onExtend}
      onDestroy={onDestroy}
      disabled={disabled}
    />
  )

  const footer = (
    <Stack gap="xs" className={classes.footer}>
      {(withAttachment || withInstance) && <Divider />}
      {attachment}
      {instance}
      <Divider />
      {solved ? (
        <Text ta="center" fw="bold">
          {t('challenge.content.already_solved')}
        </Text>
      ) : (
        <form
          onSubmit={(e) => {
            e.preventDefault()
            onSubmitFlag()
          }}
        >
          <Group justify="space-between" gap="sm" align="flex-end">
            <TextInput
              placeholder={placeholder}
              value={flag}
              disabled={disabled}
              onChange={setFlag}
              style={{ flexGrow: 1 }}
              styles={{
                input: {
                  fontFamily: theme.fontFamilyMonospace,
                },
              }}
            />
            <Button miw="6rem" type="submit" disabled={disabled}>
              {t('challenge.button.submit_flag')}
            </Button>
          </Group>
        </form>
      )}
    </Stack>
  )

  return (
    <Modal.Root
      size="45%"
      {...modalProps}
      onClose={() => {
        setFlag('')
        modalProps.onClose()
      }}
      centered
      scrollAreaComponent={CustomScrollArea}
      classNames={classes}
    >
      <Modal.Overlay />
      <Modal.Content>
        <Modal.Header>
          <Modal.Title>{title}</Modal.Title>
        </Modal.Header>
        <LoadingOverlay visible={!challenge?.content} />
        <Modal.Body pb={0}>{content}</Modal.Body>
        {footer}
      </Modal.Content>
    </Modal.Root>
  )
}

const CustomScrollArea = forwardRef<HTMLDivElement, ScrollAreaProps>((props, ref) => (
  <ScrollArea.Autosize ref={ref} {...props} type="never" />
))

export default ChallengeModal
