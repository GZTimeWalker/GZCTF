import {
  Button,
  Divider,
  Group,
  Modal,
  ModalProps,
  Stack,
  TextInput,
  Text,
  Title,
  useMantineTheme,
  ScrollAreaAutosize,
  Skeleton,
} from '@mantine/core'
import { mdiLightbulbOnOutline, mdiOpenInNew, mdiPackageVariantClosed } from '@mdi/js'
import Icon from '@mdi/react'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import InstanceEntry from '@Components/InstanceEntry'
import Markdown, { InlineMarkdown } from '@Components/MarkdownRenderer'
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

  const isContainer =
    challenge?.type === ChallengeType.StaticContainer ||
    challenge?.type === ChallengeType.DynamicContainer

  const title = (
    <Stack gap="xs">
      <Group wrap="nowrap" w="100%" justify="space-between" gap="sm">
        <Group wrap="nowrap" gap="sm">
          {tagData && (
            <Icon path={tagData.icon} size={1.2} color={theme.colors[tagData?.color][5]} />
          )}
          <Title w="calc(100% - 1.5rem)" order={4} lineClamp={1}>
            {challenge?.title ?? ''}
          </Title>
        </Group>
        <Text miw="5em" fw="bold" ff="monospace">
          {challenge?.score ?? 0} pts
        </Text>
      </Group>
      <Divider size="md" color={tagData?.color} />
    </Stack>
  )

  const content = (
    <ScrollAreaAutosize mah="50vh" maw="100%" scrollbars="y" scrollbarSize={6} type="scroll">
      {challenge?.content === undefined ? (
        <>
          <Skeleton height={14} mt={8} radius="xl" />
          <Skeleton height={14} mt={8} radius="xl" />
          <Skeleton height={14} mt={8} width="60%" radius="xl" />

          <Skeleton height={14} mt={8 + 14} radius="xl" />
          <Skeleton height={14} mt={8} radius="xl" />
          <Skeleton height={14} mt={8} width="30%" radius="xl" />
        </>
      ) : (
        <>
          <Markdown source={challenge.content ?? ''} />
          {challenge.hints && challenge.hints.length > 0 && (
            <Stack gap={2} pt="sm">
              {challenge.hints.map((hint) => (
                <Group key={hint} gap="xs" align="flex-start" wrap="nowrap">
                  <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
                  <InlineMarkdown key={hint} size="sm" maw="calc(100% - 2rem)" source={hint} />
                </Group>
              ))}
            </Stack>
          )}
        </>
      )}
    </ScrollAreaAutosize>
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

  const withInstance = isContainer && challenge?.context

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

  const cachedScrollAreaComponent = useCallback<React.FC<any>>(
    ({ style, ...props }) => <div {...props} style={{ ...style }} />,
    []
  )

  return (
    <Modal.Root
      size="40vw"
      {...modalProps}
      onClose={() => {
        setFlag('')
        modalProps.onClose()
      }}
      centered
      scrollAreaComponent={cachedScrollAreaComponent}
      classNames={classes}
    >
      <Modal.Overlay />
      <Modal.Content>
        <Modal.Header>
          <Modal.Title>{title}</Modal.Title>
        </Modal.Header>
        <Modal.Body>{content}</Modal.Body>
        {footer}
      </Modal.Content>
    </Modal.Root>
  )
}

export default ChallengeModal
