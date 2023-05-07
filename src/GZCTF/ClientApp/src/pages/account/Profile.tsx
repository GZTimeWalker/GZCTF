import { FC, useEffect, useState } from 'react'
import {
  Box,
  Stack,
  Group,
  Divider,
  Text,
  Button,
  Grid,
  TextInput,
  Paper,
  Textarea,
  Modal,
  Avatar,
  Image,
  Center,
  SimpleGrid,
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import PasswordChangeModal from '@Components/PasswordChangeModal'
import WithNavBar from '@Components/WithNavbar'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ACCEPT_IMAGE_MIME_TYPE, useIsMobile } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUser } from '@Utils/useUser'
import api, { ProfileUpdateModel } from '@Api'

const Profile: FC = () => {
  const [dropzoneOpened, setDropzoneOpened] = useState(false)
  const { user, mutate } = useUser()

  const [profile, setProfile] = useState<ProfileUpdateModel>({
    userName: user?.userName,
    bio: user?.bio,
    stdNumber: user?.stdNumber,
    phone: user?.phone,
    realName: user?.realName,
  })
  const [avatarFile, setAvatarFile] = useState<File | null>(null)

  const [disabled, setDisabled] = useState(false)

  const [mailEditOpened, setMailEditOpened] = useState(false)
  const [pwdChangeOpened, setPwdChangeOpened] = useState(false)

  const [email, setEmail] = useState('')

  const isMobile = useIsMobile()

  usePageTitle('个人信息')

  useEffect(() => {
    setProfile({
      userName: user?.userName,
      bio: user?.bio,
      stdNumber: user?.stdNumber,
      phone: user?.phone,
      realName: user?.realName,
    })
  }, [user])

  const onChangeAvatar = () => {
    if (avatarFile) {
      setDisabled(true)
      notifications.clean()
      showNotification({
        id: 'upload-avatar',
        color: 'orange',
        message: '正在上传头像',
        loading: true,
        autoClose: false,
      })

      api.account
        .accountAvatar({
          file: avatarFile,
        })
        .then(() => {
          updateNotification({
            id: 'upload-avatar',
            color: 'teal',
            message: '头像已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            autoClose: true,
          })
          setDisabled(false)
          mutate({ ...user }, { revalidate: false })
          setAvatarFile(null)
        })
        .catch(() => {
          updateNotification({
            id: 'upload-avatar',
            color: 'red',
            message: '头像更新失败',
            icon: <Icon path={mdiClose} size={1} />,
            autoClose: true,
          })
        })
        .finally(() => {
          setDisabled(false)
          setDropzoneOpened(false)
        })
    }
  }

  const onChangeProfile = () => {
    api.account
      .accountUpdate(profile)
      .then(() => {
        showNotification({
          color: 'teal',
          title: '更改成功',
          message: '个人信息已更新',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate({ ...user })
      })
      .catch(showErrorNotification)
  }

  const onChangeEmail = () => {
    if (email) {
      api.account
        .accountChangeEmail({
          newMail: email,
        })
        .then((res) => {
          if (res.data.data) {
            showNotification({
              color: 'teal',
              title: '验证邮件已发送',
              message: '请检查你的邮箱及垃圾邮件~',
              icon: <Icon path={mdiCheck} size={1} />,
            })
          } else {
            mutate({ ...user, email: email })
          }
          setMailEditOpened(false)
        })
        .catch(showErrorNotification)
    }
  }

  const context = (
    <>
      {/* Header */}
      <Box mb={5}>
        <h2>个人信息</h2>
      </Box>
      <Divider />

      {/* User Info */}
      <Stack spacing="md" m="auto" mt={15}>
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label="用户名"
              type="text"
              w="100%"
              value={profile.userName ?? 'ctfer'}
              disabled={disabled}
              onChange={(event) => setProfile({ ...profile, userName: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar
                radius={40}
                size={80}
                src={user?.avatar}
                onClick={() => setDropzoneOpened(true)}
              />
            </Center>
          </Grid.Col>
        </Grid>
        <TextInput
          label="邮箱"
          type="email"
          w="100%"
          value={user?.email ?? 'ctfer@gzti.me'}
          disabled
          readOnly
        />
        <TextInput
          label="手机号"
          type="tel"
          w="100%"
          value={profile.phone ?? ''}
          disabled={disabled}
          onChange={(event) => setProfile({ ...profile, phone: event.target.value })}
        />
        <SimpleGrid cols={2}>
          <TextInput
            label="学工号"
            type="number"
            w="100%"
            value={profile.stdNumber ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, stdNumber: event.target.value })}
          />
          <TextInput
            label="真实姓名"
            type="text"
            w="100%"
            value={profile.realName ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, realName: event.target.value })}
          />
        </SimpleGrid>
        <Textarea
          label="描述"
          value={profile.bio ?? '这个人很懒，什么都没有写'}
          w="100%"
          disabled={disabled}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setProfile({ ...profile, bio: event.target.value })}
        />
        <Box m="auto" w="100%">
          <Grid grow>
            <Grid.Col span={4}>
              <Button
                fullWidth
                color="orange"
                variant="outline"
                disabled={disabled}
                onClick={() => setMailEditOpened(true)}
              >
                更改邮箱
              </Button>
            </Grid.Col>
            <Grid.Col span={4}>
              <Button
                fullWidth
                color="orange"
                variant="outline"
                disabled={disabled}
                onClick={() => setPwdChangeOpened(true)}
              >
                更改密码
              </Button>
            </Grid.Col>
            <Grid.Col span={4}>
              <Button fullWidth disabled={disabled} onClick={onChangeProfile}>
                保存信息
              </Button>
            </Grid.Col>
          </Grid>
        </Box>
      </Stack>
    </>
  )

  return (
    <WithNavBar minWidth={0}>
      {isMobile ? (
        context
      ) : (
        <Center h="90vh">
          <Paper w="55%" maw={600} shadow="sm" pt="2%" p="5%">
            {context}
          </Paper>
        </Center>
      )}

      {/* Change Password */}
      <PasswordChangeModal
        opened={pwdChangeOpened}
        onClose={() => setPwdChangeOpened(false)}
        title="更改密码"
      />

      {/* Change Email */}
      <Modal opened={mailEditOpened} onClose={() => setMailEditOpened(false)} title="更改邮箱">
        <Stack>
          <Text>
            更改邮箱后，您将不能通过原邮箱登录。一封邮件将会发送至新邮箱，请点击邮件中的链接完成验证。
          </Text>
          <TextInput
            required
            label="新邮箱"
            type="email"
            w="100%"
            placeholder={user?.email ?? 'ctfer@gzti.me'}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
          <Group position="right">
            <Button
              variant="default"
              onClick={() => {
                setEmail(user?.email ?? '')
                setMailEditOpened(false)
              }}
            >
              取消
            </Button>
            <Button color="orange" onClick={onChangeEmail}>
              确认修改
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* Change avatar */}
      <Modal
        opened={dropzoneOpened}
        onClose={() => setDropzoneOpened(false)}
        withCloseButton={false}
      >
        <Dropzone
          onDrop={(files) => setAvatarFile(files[0])}
          onReject={() => {
            showNotification({
              color: 'red',
              title: '文件获取失败',
              message: '请检查文件格式和大小',
              icon: <Icon path={mdiClose} size={1} />,
            })
          }}
          m="0 auto 20px auto"
          miw={220}
          mih={220}
          maxSize={3 * 1024 * 1024}
          accept={ACCEPT_IMAGE_MIME_TYPE}
        >
          <Group position="center" spacing="xl" mih={240} style={{ pointerEvents: 'none' }}>
            {avatarFile ? (
              <Image fit="contain" src={URL.createObjectURL(avatarFile)} alt="avatar" />
            ) : (
              <Box>
                <Text size="xl" inline>
                  拖放图片或点击此处以选择头像
                </Text>
                <Text size="sm" color="dimmed" inline mt={7}>
                  请选择小于 3MB 的图片
                </Text>
              </Box>
            )}
          </Group>
        </Dropzone>
        <Button fullWidth variant="outline" disabled={disabled} onClick={onChangeAvatar}>
          修改头像
        </Button>
      </Modal>
    </WithNavBar>
  )
}

export default Profile
