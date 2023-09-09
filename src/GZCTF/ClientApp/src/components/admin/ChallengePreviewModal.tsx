import dayjs from 'dayjs'
import React, { FC, useEffect, useState } from 'react'
import {
  ActionIcon,
  Box,
  Button,
  Divider,
  Group,
  LoadingOverlay,
  Modal,
  ModalProps,
  Stack,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDownload, mdiLightbulbOnOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FlagPlaceholders } from '@Components/ChallengeDetailModal'
import InstanceEntry from '@Components/InstanceEntry'
import MarkdownRender, { InlineMarkdownRender } from '@Components/MarkdownRender'
import { ChallengeTagItemProps } from '@Utils/Shared'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import { useTypographyStyles } from '@Utils/useTypographyStyles'
import { ChallengeType, ChallengeUpdateModel, FileType } from '@Api'

interface ChallengePreviewModalProps extends ModalProps {
  challenge: ChallengeUpdateModel
  type: ChallengeType
  attachmentType: FileType
  tagData: ChallengeTagItemProps
}

interface FakeContext {
  closeTime: string | null
  instanceEntry: string | null
}

const ChallengePreviewModal: FC<ChallengePreviewModalProps> = (props) => {
  const { challenge, type, attachmentType, tagData, ...modalProps } = props
  const { classes: tooltipClasses } = useTooltipStyles()

  const [placeholder, setPlaceholder] = useState('')
  const [flag, setFlag] = useInputState('')

  const [context, setContext] = useState<FakeContext>({
    closeTime: null,
    instanceEntry: null,
  })

  const onCreate = () => {
    setContext({
      closeTime: dayjs().add(10, 'm').add(10, 's').toJSON(),
      instanceEntry: 'localhost:2333',
    })
  }

  const onDestroy = () => {
    setContext({
      closeTime: null,
      instanceEntry: null,
    })
  }

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault()

    showNotification({
      color: 'teal',
      title: 'flag 似乎被正确提交了！',
      message: flag,
      icon: <Icon path={mdiCheck} size={1} />,
    })
    setFlag('')
  }

  useEffect(() => {
    setPlaceholder(FlagPlaceholders[Math.floor(Math.random() * FlagPlaceholders.length)])
  }, [challenge])

  const isDynamic =
    type === ChallengeType.StaticContainer || type === ChallengeType.DynamicContainer
  const { classes, theme } = useTypographyStyles()

  return (
    <Modal
      {...modalProps}
      onClose={() => {
        setFlag('')
        modalProps.onClose()
      }}
      styles={{
        ...modalProps.styles,
        header: {
          margin: 0,
        },
        title: {
          width: '100%',
          margin: 0,
        },
      }}
      title={
        <Group w="100%" position="apart">
          <Group>
            {tagData && (
              <Icon path={tagData.icon} size={1} color={theme.colors[tagData?.color][5]} />
            )}
            <Title order={4}>{challenge?.title ?? ''}</Title>
          </Group>
          <Text fw={700} ff={theme.fontFamilyMonospace}>
            {challenge?.originalScore ?? 500} pts
          </Text>
        </Group>
      }
    >
      <Stack spacing="sm">
        <Divider />
        <Stack spacing="sm" justify="space-between" pos="relative" mih="20vh">
          <LoadingOverlay visible={!challenge} />
          <Group grow noWrap position="right" align="flex-start" spacing={2}>
            <Box className={classes.root} mih="4rem">
              {attachmentType !== FileType.None && (
                <Tooltip label="下载附件" position="left" classNames={tooltipClasses}>
                  <ActionIcon
                    variant="filled"
                    size="lg"
                    color="brand"
                    top={0}
                    right={0}
                    pos="absolute"
                    onClick={() =>
                      showNotification({
                        color: 'teal',
                        message: '假装附件已经下载了！',
                        icon: <Icon path={mdiCheck} size={1} />,
                      })
                    }
                  >
                    <Icon path={mdiDownload} size={1} />
                  </ActionIcon>
                </Tooltip>
              )}
              <MarkdownRender
                source={challenge?.content ?? ''}
                sx={{
                  '& div > p:first-child:before': {
                    content: '""',
                    float: 'right',
                    width: 45,
                    height: 45,
                  },
                }}
              />
            </Box>
          </Group>
          {challenge?.hints && challenge.hints.length > 0 && (
            <Stack spacing={2}>
              {challenge.hints.map((hint) => (
                <Group spacing="xs" align="flex-start" noWrap>
                  <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
                  <InlineMarkdownRender
                    key={hint}
                    size="sm"
                    maw="calc(100% - 2rem)"
                    source={hint}
                  />
                </Group>
              ))}
            </Stack>
          )}
          {isDynamic && (
            <InstanceEntry
              context={context}
              disabled={false}
              onCreate={onCreate}
              onProlong={onCreate}
              onDestroy={onDestroy}
            />
          )}
        </Stack>
        <Divider />
        <form onSubmit={onSubmit}>
          <TextInput
            placeholder={placeholder}
            value={flag}
            onChange={setFlag}
            styles={{
              rightSection: {
                width: 'auto',
              },
              input: {
                fontFamily: `${theme.fontFamilyMonospace}, ${theme.fontFamily}`,
              },
            }}
            rightSection={<Button type="submit">提交 flag</Button>}
            rightSectionWidth="6rem"
          />
        </form>
      </Stack>
    </Modal>
  )
}

export default ChallengePreviewModal
